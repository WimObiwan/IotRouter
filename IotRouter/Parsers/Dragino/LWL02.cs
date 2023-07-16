using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class LWL02 : TheThingsNetworkParser
{
    public LWL02(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<LHT65>>(), name)
    {
    }

    protected override ParsedData Parse(ParserData parserData)
    {
        string devEUI = parserData.GetDevEUI();
        DateTime dateTime = parserData.GetTime();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", parserData.GetPayloadValue("BAT_V").AsDecimal()),
            new("waterDurationS", parserData.GetPayloadValue("LAST_WATER_LEAK_DURATION").AsDecimal() * 60), // minutes to seconds
            new("waterStatus", parserData.GetPayloadValue("WATER_LEAK_STATUS").AsDecimal()),
            new("waterTimes", parserData.GetPayloadValue("WATER_LEAK_TIMES").AsDecimal()),
            new("RSSI", parserData.GetRSSI()),                    
        };

        return new ParsedData(devEUI, dateTime, keyValues);
    }
}