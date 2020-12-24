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

        public const double AverageInterval_lambda = 0.2;

        private double _EMAFilter_current_ignore_threshold;
        private double? _EMAFilter_average_distance;
        private TimeSpan? _averageInterval;
        private DateTime? _lastPacketDateTime;

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
            _EMAFilter_average_distance = null;
            _EMAFilter_current_ignore_threshold = 0;
        }
        
        public bool Process(ParsedData parsedData)
        {
            var kv = parsedData.KeyValues.Single(kv => kv.Key == "distance");
            decimal distance = (decimal)kv.Value;
            parsedData.KeyValues.Remove(kv);
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_raw", distance));

            DateTime dateTime = parsedData.DateTime ?? DateTime.UtcNow;

            double average_distance;
            if (_EMAFilter_average_distance.HasValue)
            {
                average_distance = _EMAFilter_average_distance.Value;
            }
            else
            {
                average_distance = (double)distance;
            }

            // Calculate some "average interval", roughly based on last 5 to 10 values
            // Derive a "EMA lambda" which is dependent of the update frequency 
            double? lambda;
            if (_lastPacketDateTime.HasValue)
            {
                TimeSpan interval = dateTime - _lastPacketDateTime.Value;
                TimeSpan averageInterval;
                if (_averageInterval.HasValue)
                    averageInterval = TimeSpan.FromTicks(
                        (long)(interval.Ticks * AverageInterval_lambda + _averageInterval.Value.Ticks * (1 - AverageInterval_lambda)));
                else
                    averageInterval = interval;
                _averageInterval = averageInterval;
                _logger.LogInformation($"Average interval is now {averageInterval}, last inerval is {interval}");
                lambda = 1.0 - Math.Pow(1 - EMAFilter_lambdahour, _averageInterval.Value.TotalHours);
            }
            else
            {
                lambda = null;
            }
            _lastPacketDateTime = dateTime;

            double absdiff = Math.Abs(average_distance - (double)distance);
            double current_ignore_threshold = _EMAFilter_current_ignore_threshold + EMAFilter_ignore_threshold;

            parsedData.KeyValues.Add(new ParsedData.KeyValue("ignore_threshold", current_ignore_threshold));
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_ignore_min", average_distance - current_ignore_threshold));
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_ignore_max", average_distance + current_ignore_threshold));

            bool above_threshold = absdiff > current_ignore_threshold;

            if (lambda.HasValue)
            {
                _EMAFilter_current_ignore_threshold = absdiff * lambda.Value + _EMAFilter_current_ignore_threshold * (1 - lambda.Value);
            }

            if (above_threshold)
            {
                _logger.LogWarning($"Ignore: Distance ({distance}) exceeds EMA ({average_distance}) using threshold ({current_ignore_threshold})");
                return true;
            }

            if (lambda.HasValue)
            {
                average_distance = (double)distance * lambda.Value + average_distance * (1 - lambda.Value);
            }

            _EMAFilter_average_distance = average_distance;

            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance", average_distance));

            // Based on distance, calculate the percentage full
            double level;
            if (LevelX.HasValue && PercentX.HasValue)
            {
                int levelX = LevelX.Value;
                double percentX = (double)PercentX.Value;
                double part1, part2;
                if (average_distance > LevelX)
                {
                    part1 = (double)Level0 - average_distance;
                    part2 = 0;
                }
                else
                {
                    part1 = Level0 - levelX;
                    part2 = (double)levelX - average_distance;
                }
                level = part1 * percentX / (Level0 - levelX) + part2 * (1 - percentX) / (levelX - Level100);
            }
            else
            {
                level = (Level0 - average_distance) / (Level0 - Level100);
            }
            parsedData.KeyValues.Add(new ParsedData.KeyValue("level", level * 100.0));

            // Based on the percentage full, calculate the water volume
            double liter = Liter100 * level;
            parsedData.KeyValues.Add(new ParsedData.KeyValue("liter", liter));

            return true;
        }
    }
}
