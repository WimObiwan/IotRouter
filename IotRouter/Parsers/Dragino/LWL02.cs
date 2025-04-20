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
        int fPort = parserData.GetFPort();
        DateTime dateTime = parserData.GetTime();

        byte[] bytes = parserData.GetPayload();
        int value;

        value = (bytes[0] << 8 | bytes[1]) & 0x3FFF;
        decimal batV = value / 1000m;//Battery,units:V
        // decimal batV_Old = parserData.GetPayloadValue("BAT_V").AsDecimal();

        int waterStatus = (bytes[0] & 0x40) != 0 ? 1 : 0;
        // int waterStatus_Old = parserData.GetPayloadValue("WATER_LEAK_STATUS").AsDecimal()),

        int waterTimes = bytes[3] << 16 | bytes[4] << 8 | bytes[5];
        // int waterTimes_Old = parserData.GetPayloadValue("WATER_LEAK_TIMES").AsDecimal();

        int waterDurationS = (bytes[6] << 16 | bytes[7] << 8 | bytes[8]) * 60;//units:min
        // int waterDurationS_Old = parserData.GetPayloadValue("LAST_WATER_LEAK_DURATION").AsDecimal() * 60;

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", batV),
            new("waterDurationS", waterDurationS), // minutes to seconds
            new("waterStatus", waterStatus),
            new("waterTimes", waterTimes),
            new("RSSI", parserData.GetRSSI()),                    
        };

        return new ParsedData(devEUI, fPort, dateTime, keyValues);
    }
}