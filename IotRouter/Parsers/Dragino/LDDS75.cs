using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class LDDS75 : Parser
{
    public LDDS75(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<LHT65>>(), name)
    {
    }

    protected override ParsedData Parse(ParserData parserData)
    {
        string devEUI = parserData.GetDevEUI();
        DateTime dateTime = parserData.GetTime();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", parserData.GetPayloadValue("batV").AsDecimal()),
            new("distance", parserData.GetPayloadValue("distanceMm").AsDecimal()),
            new("RSSI", parserData.GetRSSI()),                    
        };

        return new ParsedData(devEUI, dateTime, keyValues);
    }
}