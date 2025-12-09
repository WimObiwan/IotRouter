using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Assemblies;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers;

public class IotCreators : Parser
{
    public IotCreators(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<IotCreators>>(), name)
    {
    }

    public override ParsedData Parse(byte[] data)
    {
        _logger.LogInformation("Data: {Data}", Encoding.UTF8.GetString(data));
        
        var packet = JsonSerializer.Deserialize<Packet>(data);

        if (!(packet.reports.FirstOrDefault() is { } report))
            return null;
        // string serialNumber = report.serialNumber;
        //
        // var match = Regex.Match(serialNumber, @"^IMEI:(\d+)$");
        // if (!match.Success)
        //     throw new Exception("Unrecognized serialNumber");
        // string devEui = match.Groups[1].Value;

        // var dateTime = DateTime.UnixEpoch.AddMilliseconds(report.timestamp);

        var payloadText = report.value;
        var payload = Convert.FromHexString(payloadText);

        // http://wiki.dragino.com/xwiki/bin/view/Main/User%20Manual%20for%20LoRaWAN%20End%20Nodes/NDDS75%20NB-IoT%20Distance%20Detect%20Sensor%20User%20Manual/#H3.200BAccessNB-IoTModule
        // f86778705454507800980ce1090100030c64c7969c030cac165b00030aac165b0002e664b0786502ea64b074e102ea64b0715d02e964b06dd902ea64b06a5502e964b066d1
        // ^               ^   ^   ^ ^ ^ ^   ^       ^   ^       ^

        if (payload[1] == 0x60) // PS
            return ParsePSNB(payload);

        // Fallback old parsing

        string devEui;
        ushort battery;
        byte signal = payload[12];
        ushort distance;
        uint timestamp;
        if (payload.Length <= 69)
        {
            devEui = Convert.ToHexString(payload[0..8]); // must start with F
            //ushort version = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[8..10]));
            battery = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[10..12]));
            signal = payload[12];
            //byte mod = payload[13];
            //byte interrupt = payload[14];
            distance = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[15..17]));
            timestamp = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(payload[17..21]));
        }
        else
        {                
            devEui = Convert.ToHexString(payload[0..8]); // must start with F
            ushort version = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[16..18]));
            battery = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[18..20]));
            signal = payload[20];
            //byte mod = payload[21];
            //byte interrupt = payload[22];
            //byte reserved = payload[23];
            distance = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[24..26]));
            timestamp = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(payload[26..30]));
        }

        DateTime dateTime;
        if (timestamp != 0) 
            dateTime = DateTime.UnixEpoch.AddSeconds(timestamp);
        else
            dateTime = DateTime.UtcNow;
        
        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", battery * 0.001m),
            new("distance", Convert.ToDecimal(distance)),
            new("distance_raw", Convert.ToDecimal(distance)),
            new("RSSI", Convert.ToDecimal(SignalToRssi(signal)))                    
        };

        return new ParsedData(devEui, 0, dateTime, keyValues);
    }

    private ParsedData ParsePSNB(byte[] payload)
    {
        string devEui = Convert.ToHexString(payload[0..8]); // must start with F
        ushort version = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[16..18]));
        ushort battery = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[18..20]));
        byte signal = payload[20];
        //byte in1 = payload[21];
        //byte in2 = payload[22];
        //byte gpioExitLevel = payload[23];
        //byte gpioExitFlag = payload[24];
        //byte idcAlarm = payload[25];
        //byte vdcAlarm = payload[26];
        ushort probeMod = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[25..27]));
        ushort amperage = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[27..29]));
        ushort voltage = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[29..31]));
        uint timestamp = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(payload[31..35]));

        DateTime dateTime;
        if (timestamp != 0) 
            dateTime = DateTime.UnixEpoch.AddSeconds(timestamp);
        else
            dateTime = DateTime.UtcNow;

        decimal pressure = amperage / 1000.0m; // 4.0mA - 20.0mA
        decimal distance = (pressure - 4.0m) / (20.0m - 4.0m) * 5000.0m; // 5m cable length
        
        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("batV", battery * 0.001m),
            new("pressure", pressure),
            new("distance", distance),
            new("RSSI", Convert.ToDecimal(SignalToRssi(signal)))                    
        };

        return new ParsedData(devEui, 0, dateTime, keyValues);
    }

    private class Packet
    {
        public class Report
        {
            //public string serialNumber { get; init; }
            //public long timestamp { get; init; }
            public string value { get; init; }
        }

        public Report[] reports { get; init; }
    }

    private int SignalToRssi(int signal) =>
        signal switch
        {
            0 => -113,
            1 => -111,
            > 1 and < 31 => -111 + (signal * (-51 - -111) / 30),
            31 => -51,
            _ => -1
        };
}

