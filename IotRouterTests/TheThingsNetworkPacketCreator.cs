using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

namespace IotRouterTests;

public static class TheThingsNetworkPacketCreator
{
    public record TheThingsNetworkPacket
    {
        public record EndDeviceIds
        {
            public string dev_eui { init; get; }
        }

        public record UplinkMessage
        {
            public DateTime received_at { init; get; }
            public string frm_payload { init; get; }
            public string decoded_payload { init; get; }
            public RxMetaData[] rx_metadata { init; get; }
        }

        public record RxMetaData
        {
            public int rssi { init; get; }
        }

        public EndDeviceIds end_device_ids { init; get; }
        public UplinkMessage uplink_message { init; get; }
    }

    public static byte[] Create(
        string devEUI,
        DateTime receivedAt,
        int rssi,
        string base64Payload
    )
    {
        var packet = new TheThingsNetworkPacket
        {
            end_device_ids = new TheThingsNetworkPacket.EndDeviceIds
            {
                dev_eui = devEUI
            },
            uplink_message = new TheThingsNetworkPacket.UplinkMessage
            {
                received_at = receivedAt,
                frm_payload = base64Payload,
                decoded_payload = "",
                rx_metadata = [
                    new TheThingsNetworkPacket.RxMetaData
                    {
                        rssi = rssi
                    }
                ]
            }
        };

        var jsonPacket = System.Text.Json.JsonSerializer.Serialize(packet);
        return Create(jsonPacket);
    }

    public static byte[] Create(
        string rawJson
    )
    {
        return Encoding.UTF8.GetBytes(rawJson);
    }
}
