using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
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
        string devEui = Convert.ToHexString(payload[0..8]);
        //ushort version = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[8..10]));
        ushort battery = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[10..12]));
        byte signal = payload[12];
        //byte mod = payload[13];
        //byte interrupt = payload[14];
        ushort distance = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(payload[15..17]));
        uint timestamp = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(payload[17..21]));

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

        return new ParsedData(devEui, dateTime, keyValues);
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

