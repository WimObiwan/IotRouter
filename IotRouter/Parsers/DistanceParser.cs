using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public class DistanceParser : Parser
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

            return new ParsedData(devEUI, dateTime, keyValues);
        }
    }
}
