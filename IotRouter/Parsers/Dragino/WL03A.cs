using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class WL03A : TheThingsNetworkParser
{
    public WL03A(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<WL03A>>(), name)
    {
    }

    protected override ParsedData Parse(ParserData parserData)
    {
        string devEUI = parserData.GetDevEUI();
        int fPort = parserData.GetFPort();
        DateTime dateTime = parserData.GetTime();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("RSSI", parserData.GetRSSI()),                    
        };

        byte[] bytes = parserData.GetPayload();
        if (fPort == 2)
        {
            int waterStatus = (bytes[0] & 0x1) != 0 ? 1 : 0;
            int waterTimes = bytes[1] << 16 | bytes[2] << 8 | bytes[3];
            int waterDurationS = bytes[4] << 16 | bytes[5] << 8 | bytes[6]; //units:seconds

            keyValues.AddRange(
                [
                    new("waterStatus", waterStatus),
                    new("waterTimes", waterTimes),
                    new("waterDurationS", waterDurationS)
                ]);
        }
        else if (fPort == 5)
        {
            int value = bytes[5] << 8 | bytes[6];
            decimal batV = value / 1000m;//Battery,units:V
            keyValues.Add(new("batV", batV));
        }

        return new ParsedData(devEUI, fPort, dateTime, keyValues);
    }
}