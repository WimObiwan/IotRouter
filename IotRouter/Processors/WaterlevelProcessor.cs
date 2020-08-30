using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter
{
    public class WaterlevelProcessor : IProcessor
    {
        ILogger<WaterlevelProcessor> _logger;

        public string Name { get; private set; }
        public int Level0 { get; private set; }
        public int Level100 { get; private set; }
        public int Liter100 { get; private set; }
        public int? LevelX { get; private set; }
        public decimal? PercentX { get; private set; }
        
        public double EMAFilter_lambdahour { get; private set; }
        public int EMAFilter_ignore_threshold { get; private set; }
        public int EMAFilter_drop_threshold { get; private set; }
        public int EMAFilter_drop_timeoutsec { get; private set; }

        public decimal? EMAFilter_state { get; private set; }
        public DateTime? EMAFilter_droptill { get; private set; }
        public TimeSpan? AverageInterval { get; private set; }
        public const double AverageInterval_lambda = 0.2;
        public DateTime? LastPacketDateTime { get; private set; }

        public WaterlevelProcessor(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetService<ILogger<WaterlevelProcessor>>();

            Name = name;
            Level0 = config.GetValue("Level0", 2000);
            Level100 = config.GetValue("Level100", 300);
            Liter100 = config.GetValue("Liter100", 10000);
            LevelX = config.GetValue<int?>("LevelX");
            PercentX = config.GetValue<decimal?>("PercentX");
            if (PercentX.HasValue) {
                PercentX = PercentX.Value / 100m;
            }

            int span = Math.Abs(Level0 - Level100); // 1700

            EMAFilter_lambdahour = config.GetValue("EMAFilter_lambdahour", 0.95); // 12" --> 0.01
            EMAFilter_ignore_threshold = config.GetValue("EMAFilter_ignore_threshold", span / 30); // 1700 / 30 = ~55
            EMAFilter_drop_threshold = config.GetValue("EMAFilter_drop_threshold", span / 3); // 1700 / 3 = ~550
            EMAFilter_drop_timeoutsec = config.GetValue("EMAFilter_drop_timeoutsec", 1800);
            EMAFilter_state = null;
            EMAFilter_droptill = null;
        }
        
        public bool Process(ParsedData parsedData)
        {
            var kv = parsedData.KeyValues.Single(kv => kv.Key == "distance");
            decimal distance = (decimal)kv.Value;
            parsedData.KeyValues.Remove(kv);
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_raw", distance));

            DateTime dateTime = parsedData.DateTime ?? DateTime.UtcNow;

            // Calculate some "average interval", roughly based on last 5 to 10 values
            if (LastPacketDateTime.HasValue)
            {
                TimeSpan interval = dateTime - LastPacketDateTime.Value;
                TimeSpan averageInterval;
                if (AverageInterval.HasValue)
                    averageInterval = TimeSpan.FromTicks(
                        (long)(interval.Ticks * AverageInterval_lambda + AverageInterval.Value.Ticks * (1 - AverageInterval_lambda)));
                else
                    averageInterval = interval;
                AverageInterval = averageInterval;
                _logger.LogInformation($"Average interval is now {averageInterval}, last inerval is {interval}");
            }
            LastPacketDateTime = dateTime;

            if (EMAFilter_state.HasValue)
            {
                decimal EMAFilter_oldstate = EMAFilter_state.Value;
                decimal absdiff = Math.Abs(EMAFilter_oldstate - distance);

                if (absdiff > EMAFilter_drop_threshold)
                {
                    if (!EMAFilter_droptill.HasValue)
                    {
                        _logger.LogWarning($"Drop: Distance ({distance}) exceeds EMA ({EMAFilter_oldstate}) using threshold ({EMAFilter_drop_threshold}), resetting timeout");
                        EMAFilter_droptill = dateTime.AddSeconds(EMAFilter_drop_timeoutsec);
                        return true;
                    }
                    else if (dateTime < EMAFilter_droptill)
                    {
                        _logger.LogWarning($"Drop: Distance ({distance}) exceeds EMA ({EMAFilter_oldstate}) using threshold ({EMAFilter_drop_threshold}), drop till {EMAFilter_droptill:T}");
                        return true;
                    }

                    _logger.LogWarning($"No longer drop: Distance ({distance}) exceeds EMA ({EMAFilter_oldstate}) using threshold ({EMAFilter_drop_threshold}), was dropped till {EMAFilter_droptill:T}");
                }


                // Derive a "lambda" to 
                if (AverageInterval.HasValue)
                {
                    double lambda = 1.0 - Math.Pow(1 - EMAFilter_lambdahour, AverageInterval.Value.TotalHours); 

                    decimal EMAFilter_newstate = distance * (decimal)lambda + EMAFilter_oldstate * (1 - (decimal)lambda);
                    EMAFilter_state = EMAFilter_newstate;

                    if (absdiff > EMAFilter_ignore_threshold)
                    {
                        _logger.LogWarning($"Ignore: Distance ({distance}) exceeds EMA ({EMAFilter_oldstate}) using threshold ({EMAFilter_ignore_threshold})");
                        return true;
                    }
                    _logger.LogInformation($"Distance ({distance}) does not exceed EMA ({EMAFilter_oldstate}), threshold ({EMAFilter_ignore_threshold})");
                    distance = EMAFilter_newstate;
                }

                EMAFilter_droptill = null;
            }
            else
            {
                EMAFilter_state = distance;
            }

            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance", distance));

            decimal level;
            if (LevelX.HasValue && PercentX.HasValue)
            {
                int levelX = LevelX.Value;
                decimal percentX = PercentX.Value;
                decimal part1, part2;
                if (distance > LevelX)
                {
                    part1 = Level0 - distance;
                    part2 = 0;
                }
                else
                {
                    part1 = Level0 - levelX;
                    part2 = levelX - distance;
                }
                level = part1 * percentX / (Level0 - levelX) + part2 * (1 - percentX) / (levelX - Level100);
            }
            else
            {
                level = (Level0 - distance) / (Level0 - Level100);
            }

            decimal liter = Liter100 * level;

            parsedData.KeyValues.Add(new ParsedData.KeyValue("level", level * 100m));
            parsedData.KeyValues.Add(new ParsedData.KeyValue("liter", liter));

            return true;
        }
    }
}
