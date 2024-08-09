using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class LDDS75 : TheThingsNetworkParser
{
    public LDDS75(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<LHT65>>(), name)
    {
    }

    protected override ParsedData Parse(ParserData parserData)
    {
        string devEUI = parserData.GetDevEUI();
        DateTime dateTime = parserData.GetTime();

        byte[] bytes = parserData.GetPayload();
        int value;
        
        value = (bytes[0] << 8 | bytes[1]) & 0x3FFF;
        decimal batV = value / 1000m;
        // decimal batV_Old = parserData.GetPayloadValue("batV").AsDecimal();

        decimal distanceMm = bytes[2] << 8 | bytes[3];
        // decimal distanceMm_Old = parserData.GetPayloadValue("distanceMm").AsDecimal();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", batV),
            new("distance", distanceMm),
            new("RSSI", parserData.GetRSSI()),                    
        };

        return new ParsedData(devEUI, dateTime, keyValues);
    }
}