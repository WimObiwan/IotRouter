using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter
{
    public class WaterlevelParser : Parser
    {
        public WaterlevelParser(IServiceProvider serviceProvider, IConfigurationSection config, string name)
            : base(serviceProvider.GetService<ILogger<WaterlevelParser>>(), name)
        {
        }
        
        protected override ParsedData Parse(ParserData parserData)
        {
            string devEUI = parserData.GetDevEUI();
            DateTime dateTime = parserData.GetTime();

            var keyValues = new List<ParsedData.KeyValue>()
            {
                new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),
                new ParsedData.KeyValue("waterlevel", parserData.GetPayloadValue("distance").AsDecimal() - 5m),
            };

            return new ParsedData(devEUI, dateTime, keyValues);
        }
    }
}
