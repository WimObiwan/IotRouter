using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers.Dragino;

public class LHT65 : TheThingsNetworkParser
{
    public LHT65(IServiceProvider serviceProvider, IConfigurationSection config, string name)
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
        // decimal batV_Old = parserData.GetPayloadValue("BatV").AsDecimal();

        value = bytes[2] << 8 | bytes[3];
        if ((bytes[2] & 0x80) != 0)
            value = (int)(value | 0xFFFF0000);
        decimal tempC_SHT = Math.Round(value / 100m, 2);
        // decimal tempC_SHT_Old = parserData.GetPayloadValue("TempC_SHT").AsDecimal();

        value = bytes[4] << 8 | bytes[5];
        decimal hum_SHT = Math.Round(value / 10m, 1);
        // decimal hum_SHT_Old = parserData.GetPayloadValue("Hum_SHT").AsDecimal();

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new ParsedData.KeyValue("BatV", batV),
            new ParsedData.KeyValue("Air.Temperature", tempC_SHT),
            new ParsedData.KeyValue("Air.Humidity", hum_SHT),
            new ParsedData.KeyValue("RSSI", parserData.GetRSSI()),                    
        };

        value = bytes[7] << 8 | bytes[8];
        if ((bytes[7] & 0x80) != 0)
            value = (int)(value | 0xFFFF0000);

        if (value != 0x7FFF)
        {
            decimal temp_DS = Math.Round(value / 100m, 2);
            if (temp_DS >= -30.0m && temp_DS <= 80.0m)
                keyValues.Add(new ParsedData.KeyValue("Soil.Temperature", temp_DS));
        }

        return new ParsedData(devEUI, dateTime, keyValues);
    }
}
