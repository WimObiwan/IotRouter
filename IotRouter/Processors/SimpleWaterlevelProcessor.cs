using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public class SimpleWaterlevelProcessor : IProcessor
    {
        ILogger<SimpleWaterlevelProcessor> _logger;

        public string Name { get; }
        private readonly int? _level0;
        private readonly int? _level100;
        private readonly int? _liter100;
        private readonly int? _levelX;
        private readonly decimal? _percentX;
        
        public SimpleWaterlevelProcessor(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<SimpleWaterlevelProcessor>>();

            Name = name;
            _level0 = config.GetValue<int?>("Level0");
            _level100 = config.GetValue<int?>("Level100");
            _liter100 = config.GetValue<int?>("Liter100");
            _levelX = config.GetValue<int?>("LevelX");
            _percentX = config.GetValue<decimal?>("PercentX");
            if (_percentX.HasValue) {
                _percentX = _percentX.Value / 100m;
            }
        }
        
        public Task<bool> Process(ParsedData parsedData)
        {
            var kv = parsedData.KeyValues.Single(kv => kv.Key == "distance");
            decimal distance = (decimal)kv.Value;
            parsedData.KeyValues.Remove(kv);
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance_raw", distance));
            if (distance == 0)
            {
                _logger.LogWarning("Ignoring distance=0");
                return Task.FromResult(true);
            }
            
            parsedData.KeyValues.Add(new ParsedData.KeyValue("distance", distance));

            double? level = GetLevelFromDistance((double)distance);
            if (level.HasValue)
            {
                parsedData.KeyValues.Add(new ParsedData.KeyValue("level", level.Value * 100.0));

                // Based on the percentage full, calculate the water volume
                if (_liter100.HasValue)
                {
                    double liter = _liter100.Value * level.Value;
                    parsedData.KeyValues.Add(new ParsedData.KeyValue("liter", liter));
                }
            }

            return Task.FromResult(true);
        }

        private double? GetLevelFromDistance(double distance)
        {
            if (!_level0.HasValue || !_level100.HasValue)
                return null;

            int level0 = _level0.Value;
            int level100 = _level100.Value;
            
            double level;
            if (_levelX.HasValue && _percentX.HasValue)
            {
                int levelX = _levelX.Value;
                double percentX = (double)_percentX.Value;
                double part1, part2;
                if (distance > _levelX)
                {
                    part1 = (double)level0 - distance;
                    part2 = 0;
                }
                else
                {
                    part1 = level0 - levelX;
                    part2 = (double)levelX - distance;
                }
                level = part1 * percentX / (level0 - levelX) + part2 * (1 - percentX) / (levelX - level100);
            }
            else
            {
                level = (level0 - distance) / (level0 - level100);
            }

            return level;
        }
    }
}
