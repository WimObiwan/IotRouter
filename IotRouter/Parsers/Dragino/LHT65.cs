using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino
{
    public class LHT65 : Parser
    {
        public LHT65(IServiceProvider serviceProvider, IConfigurationSection config, string name)
            : base(serviceProvider.GetService<ILogger<LHT65>>(), name)
        {
        }

        protected override ParsedData Parse(ParserData parserData)
        {
            string devEUI = parserData.GetDevEUI();
            DateTime dateTime = parserData.GetTime();

            var keyValues = new List<ParsedData.KeyValue>()
            {
                new ParsedData.KeyValue("BatV", parserData.GetPayloadValue("BatV").AsDecimal()),
                new ParsedData.KeyValue("Air.Temperature", parserData.GetPayloadValue("TempC_SHT").AsDecimal()),
                new ParsedData.KeyValue("Air.Humidity", parserData.GetPayloadValue("Hum_SHT").AsDecimal()),
                new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),                    
            };

            if (parserData.TryGetPayloadValue("TempC_DS", out ParserValue parserValue) && !parserValue.IsNull())
            {
                keyValues.Add(new ParsedData.KeyValue("Soil.Temperature", parserValue.AsDecimal()));
            }

            return new ParsedData(devEUI, dateTime, keyValues);
        }
    }
}
