using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public class DistanceParser : TheThingsNetworkParser
    {

        public DistanceParser(IServiceProvider serviceProvider, IConfigurationSection config, string name)
            : base(serviceProvider.GetService<ILogger<DistanceParser>>(), name)
        {
        }
        
        protected override ParsedData Parse(ParserData parserData)
        {
            string devEUI = parserData.GetDevEUI();
            DateTime dateTime = parserData.GetTime();

            decimal distance = parserData.GetPayloadValue("distance").AsDecimal();

            var keyValues = new List<ParsedData.KeyValue>()
            {
                new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),
                new ParsedData.KeyValue("distance", distance),
            };

            ParserValue humidityValue, temperatureValue;
            if (parserData.TryGetPayloadValue("humid", out humidityValue)
                && parserData.TryGetPayloadValue("temp", out temperatureValue))
            {
                decimal humidity = humidityValue.AsDecimal();
                decimal temperature = temperatureValue.AsDecimal();
                if (humidity != 0.0m && temperature != 0.0m)
                {
                    keyValues.Add(new ParsedData.KeyValue("humid", humidity));
                    keyValues.Add(new ParsedData.KeyValue("temp", temperature));
                }
            }

            return new ParsedData(devEUI, dateTime, keyValues);
        }
    }
}
