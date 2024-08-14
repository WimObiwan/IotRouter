using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class SE01 : TheThingsNetworkParser
{
    public SE01(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<SE01>>(), name)
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
        // decimal batV_Old = parserData.GetPayloadValue("BatV").AsDecimal();

        value = bytes[4] << 8 | bytes[5];
        decimal soilMoisturePrc = value / 100m;
        // decimal soilMoisturePrc_Old = parserData.GetPayloadValue("water_SOIL").AsDecimal();

        value = bytes[6] << 8 | bytes[7];
        decimal soilTemperature = value / 100m;
        // decimal soilTemperature_Old = parserData.GetPayloadValue("temp_SOIL").AsDecimal();

        value = bytes[8] << 8 | bytes[9];
        decimal soilConductivity = value;
        // decimal soilConductivity_Old = parserData.GetPayloadValue("conduct_SOIL").AsDecimal();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", batV),
            new("soilMoisturePrc", soilMoisturePrc),
            new("soilTemperature", soilTemperature),
            new("soilConductivity", soilConductivity),
            new("RSSI", parserData.GetRSSI()),                    
        };

        return new ParsedData(devEUI, dateTime, keyValues);
    }
}