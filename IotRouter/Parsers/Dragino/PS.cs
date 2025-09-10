using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class PS : TheThingsNetworkParser
{
    public PS(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<PS>>(), name)
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

            int probeModel = bytes[2] << 8 | bytes[3];

            decimal pressure = (bytes[4] << 8 | bytes[5]) / 1000.0m;
            // 4.0mA - 20.0mA

            decimal distanceMm = (pressure - 4.0m) / (20.0m - 4.0m) * 5000.0m;
            // 5m cable length

            keyValues.AddRange(
                [
                    new("batV", batV),
                    new("pressure", pressure),
                    new("distance", distanceMm)
                ]);
        }

        return new ParsedData(devEUI, fPort, dateTime, keyValues);
    }
}