using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter.Parsers.Dragino
{
    public class LGT92 : Parser
    {
        public LGT92(IServiceProvider serviceProvider, IConfigurationSection config, string name)
            : base(serviceProvider.GetService<ILogger<LGT92>>(), name)
        {
        }
        
        protected override ParsedData Parse(ParserData parserData)
        {
            string devEUI = parserData.GetDevEUI();
            DateTime dateTime = parserData.GetTime();

            decimal batV = parserData.GetPayloadValue("batV").AsDecimal();
            var keyValues = new List<ParsedData.KeyValue>()
            {
                new ParsedData.KeyValue("BatV", batV),
                new ParsedData.KeyValue("BatPrc", (batV - 3.40m) / 0.60m * 100),
                //new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),
            };

            return new ParsedData(devEUI, dateTime, keyValues);
        }
    }
}
