using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter
{
    public class WaterlevelProcessor : IProcessor
    {
        class State
        {
            public double EMAFilter_current_ignore_threshold { get; set; }
            public double? EMAFilter_average_distance { get; set; }
            [JsonConverter(typeof(TimeSpanConverter))]
            public TimeSpan? AverageInterval { get; set; }
            public DateTime? LastPacketDateTime { get; set; }

            public override string ToString()
            {
                return $"EMAFilter_current_ignore_threshold={EMAFilter_current_ignore_threshold}, EMAFilter_average_distance={EMAFilter_average_distance}, "
                    + $"AverageInterval={AverageInterval}, LastPacketDateTime={LastPacketDateTime}";
            }
        }

        ILogger<WaterlevelProcessor> _logger;
        IStateService _stateProvider;

        public string Name { get; private set; }
        public int Level0 { get; private set; }
        public int Level100 { get; private set; }
        public int Liter100 { get; private set; }
        public int? LevelX { get; private set; }
        public decimal? PercentX { get; private set; }
        
        public double EMAFilter_lambdahour { get; private set; }
        public int EMAFilter_ignore_threshold { get; private set; }

        public const double AverageInterval_lambda = 0.2;

        public WaterlevelProcessor(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<WaterlevelProcessor>>();
            _stateProvider = serviceProvider.GetRequiredService<IStateService>();

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
        }
        
        public async Task<bool> Process(ParsedData parsedData)
        {
            var kv = parsedData.KeyValues.Single(kv => kv.Key == "distance");
            decimal distance = (decimal)kv.Value;
            parsedData.KeyValues.Remove(kv);
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_raw", distance));
            double level_raw = GetLevelFromDistance((double)distance);
            parsedData.KeyValues.Add(new ParsedData.KeyValue("level_raw", level_raw * 100.0));

            DateTime dateTime = parsedData.DateTime ?? DateTime.UtcNow;

            string stateContext = nameof(WaterlevelProcessor) + "#" + parsedData.DevEUI;
            State state = await _stateProvider.LoadStateAsync<State>(stateContext,
                () => new State
                {
                    EMAFilter_average_distance = null,
                    EMAFilter_current_ignore_threshold = 0
                });
            _logger.LogInformation($"State: {state}");

            double average_distance;
            if (state.EMAFilter_average_distance.HasValue)
            {
                average_distance = state.EMAFilter_average_distance.Value;
            }
            else
            {
                average_distance = (double)distance;
            }

            // Calculate some "average interval", roughly based on last 5 to 10 values
            // Derive a "EMA lambda" which is dependent of the update frequency 
            double? lambda;
            if (state.LastPacketDateTime.HasValue)
            {
                TimeSpan interval = dateTime - state.LastPacketDateTime.Value;
                TimeSpan averageInterval;
                if (state.AverageInterval.HasValue)
                    averageInterval = TimeSpan.FromTicks(
                        (long)(interval.Ticks * AverageInterval_lambda + state.AverageInterval.Value.Ticks * (1 - AverageInterval_lambda)));
                else
                    averageInterval = interval;
                state.AverageInterval = averageInterval;
                _logger.LogInformation($"Average interval is now {averageInterval}, last inerval is {interval}");
                lambda = 1.0 - Math.Pow(1 - EMAFilter_lambdahour, state.AverageInterval.Value.TotalHours);
            }
            else
            {
                lambda = null;
            }
            state.LastPacketDateTime = dateTime;

            double absdiff = Math.Abs(average_distance - (double)distance);
            double current_ignore_threshold = state.EMAFilter_current_ignore_threshold + EMAFilter_ignore_threshold;

            parsedData.KeyValues.Add(new ParsedData.KeyValue("ignore_threshold", current_ignore_threshold));
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_ignore_min", average_distance - current_ignore_threshold));
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_ignore_max", average_distance + current_ignore_threshold));

            bool above_threshold = absdiff > current_ignore_threshold;

            if (lambda.HasValue)
            {
                state.EMAFilter_current_ignore_threshold = absdiff * lambda.Value + state.EMAFilter_current_ignore_threshold * (1 - lambda.Value);
            }

            if (above_threshold)
            {
                _logger.LogWarning($"Ignore: Distance ({distance}) exceeds EMA ({average_distance}) using threshold ({current_ignore_threshold})");
            }
            else
            {
                if (lambda.HasValue)
                {
                    average_distance = (double)distance * lambda.Value + average_distance * (1 - lambda.Value);
                }

                state.EMAFilter_average_distance = average_distance;

                parsedData.KeyValues.Add(new ParsedData.KeyValue("distance", average_distance));

                double level = GetLevelFromDistance(average_distance);
                parsedData.KeyValues.Add(new ParsedData.KeyValue("level", level * 100.0));

                // Based on the percentage full, calculate the water volume
                double liter = Liter100 * level;
                parsedData.KeyValues.Add(new ParsedData.KeyValue("liter", liter));
            }

            await _stateProvider.StoreStateAsync(stateContext, state);
            
            return true;
        }

        private double GetLevelFromDistance(double distance)
        {
            double level;
            if (LevelX.HasValue && PercentX.HasValue)
            {
                int levelX = LevelX.Value;
                double percentX = (double)PercentX.Value;
                double part1, part2;
                if (distance > LevelX)
                {
                    part1 = (double)Level0 - distance;
                    part2 = 0;
                }
                else
                {
                    part1 = Level0 - levelX;
                    part2 = (double)levelX - distance;
                }
                level = part1 * percentX / (Level0 - levelX) + part2 * (1 - percentX) / (levelX - Level100);
            }
            else
            {
                level = (Level0 - distance) / (Level0 - Level100);
            }

            return level;
        }
    }
}
