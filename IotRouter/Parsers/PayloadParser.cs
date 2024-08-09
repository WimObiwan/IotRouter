// using System;
// using System.Collections.Generic;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;

// namespace IotRouter
// {
//     public class PayloadParser : TheThingsNetworkParser
//     {
//         public PayloadParser(IServiceProvider serviceProvider, IConfigurationSection config, string name)
//             : base(serviceProvider.GetService<ILogger<PayloadParser>>(), name)
//         {
//         }

//         protected override ParsedData Parse(ParserData parserData)
//         {
//             string devEUI = parserData.GetDevEUI();
//             DateTime dateTime = parserData.GetTime();

//             var keyValues = new List<ParsedData.KeyValue>()
//             {
//                 new ParsedData.KeyValue("BatV", parserData.GetPayloadValue("BatV").AsDecimal()),
//                 new ParsedData.KeyValue("Air.Temperature", parserData.GetPayloadValue("TempC_SHT").AsDecimal()),
//                 new ParsedData.KeyValue("Air.Humidity", parserData.GetPayloadValue("Hum_SHT").AsDecimal()),
//                 new ParsedData.KeyValue("Soil.Temperature", parserData.GetPayloadValue("TempC_DS").AsDecimal()),
//                 new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),                    
//             };

//             return new ParsedData(devEUI, dateTime, keyValues);
//         }
//     }
// }
