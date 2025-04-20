using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class LDDS75 : TheThingsNetworkParser
{
    public LDDS75(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<LDDS75>>(), name)
    {
    }

    protected override ParsedData Parse(ParserData parserData)
    {
        string devEUI = parserData.GetDevEUI();
        int fPort = parserData.GetFPort();
        DateTime dateTime = parserData.GetTime();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("RSSI", parserData.GetRSSI())
        };

        byte[] bytes = parserData.GetPayload();
        if (fPort == 2)
        {
            int value;
            
            value = (bytes[0] << 8 | bytes[1]) & 0x3FFF;
            decimal batV = value / 1000m;
            // decimal batV_Old = parserData.GetPayloadValue("batV").AsDecimal();

            decimal distanceMm = bytes[2] << 8 | bytes[3];
            // decimal distanceMm_Old = parserData.GetPayloadValue("distanceMm").AsDecimal();

            keyValues.AddRange(
                [
                    new("batV", batV),
                    new("distance", distanceMm)
                ]);
        }

        return new ParsedData(devEUI, fPort, dateTime, keyValues);
    }
}