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

            byte[] bytes = parserData.GetPayload();
            int value;

            value = bytes[0] + ((bytes[1] & 0x1F) << 8);
            int distance = value;
            decimal distance_Old = parserData.GetPayloadValue("distance").AsDecimal();

            //var tries = bytes[2];
            value = bytes[3] + (((bytes[1] & 0xC0) >> 6) << 8);
            decimal humidity = Math.Round(value / 10m, 1);
            decimal humidity_Old = parserData.GetPayloadValue("humid").AsDecimal();

            value = bytes[4] + (((bytes[1] & 0x20) >> 5) << 8);
            decimal temperature = Math.Round(value / 10m, 1);
            decimal temperature_Old = parserData.GetPayloadValue("temp").AsDecimal();


            var keyValues = new List<ParsedData.KeyValue>()
            {
                new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),
                new ParsedData.KeyValue("distance", distance),
                new ParsedData.KeyValue("humid", humidity),
                new ParsedData.KeyValue("temp", temperature)
            };

            return new ParsedData(devEUI, dateTime, keyValues);
        }
    }
}
