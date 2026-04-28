using System.Text;
using IotRouter.Parsers;
using IotRouter.Parsers.Dragino;
using Microsoft.Extensions.Logging;
using Moq;

namespace IotRouterTests;

public class DDS75NBTest
{
    [Fact]
    public void Test()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var logger = Mock.Of<ILogger<IotCreators>>();
        serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

        var parser = new IotCreators(serviceProviderMock.Object, null, "test");

        var rawJson = """
        {"reports":[{"value":"F861275079854879F90140511276772409820E3909010000010168C1BCEB000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"}]}
        """;

        var result = parser.Parse(Encoding.UTF8.GetBytes(rawJson));
        Assert.Equal("F861275079854879", result.DevEUI);
        Assert.Equal(0, result.FPort);
        Assert.Equal(638931240750000000, result.DateTime?.Ticks);
        var keyValues = result.KeyValues.ToDictionary(kv => kv.Key, kv => kv.Value);        
        Assert.Equal(257m, (decimal)keyValues["distance"]);
        Assert.Equal(3.641m, (decimal)keyValues["batV"]);
        Assert.Equal(-93m, (decimal)keyValues["RSSI"]);
    }

    [Fact]
    public void Test2()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var logger = Mock.Of<ILogger<IotCreators>>();
        serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

        var parser = new IotCreators(serviceProviderMock.Object, null, "test");

        var rawJson = """
        {"reports":[{"value":"F860631071942560F90140511276772609840E180B01000002A869F106FF02A869F106F302A869F0EAD3029B69F0CEB3029369F0B293027E69F09670027D69F07A53027D69F05E33027A69F04213"}]}
        """;

        var result = parser.Parse(Encoding.UTF8.GetBytes(rawJson));
        Assert.Equal("F860631071942560", result.DevEUI);
        Assert.Equal(0, result.FPort);
        Assert.Equal(639130004470000000, result.DateTime?.Ticks);
        var keyValues = result.KeyValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(680m, (decimal)keyValues["distance"]);
        Assert.Equal(3.608m, (decimal)keyValues["batV"]);
        Assert.Equal(-89m, (decimal)keyValues["RSSI"]);
    }

    [Fact]
    public void InvalidTimestampFallsBackToReportTimestamp()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var logger = Mock.Of<ILogger<IotCreators>>();
        serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

        var parser = new IotCreators(serviceProviderMock.Object, null, "test");

        var rawJson = """
        {"reports":[{"timestamp":1777403647000,"value":"F860631071942560F90140511276772609840E180B01000002A800000000FF02A869F106F302A869F0EAD3029B69F0CEB3029369F0B293027E69F09670027D69F07A53027D69F05E33027A69F04213"}]}
        """;

        var result = parser.Parse(Encoding.UTF8.GetBytes(rawJson));
        Assert.Equal("F860631071942560", result.DevEUI);
        Assert.Equal(0, result.FPort);
        Assert.Equal(639130004470000000, result.DateTime?.Ticks);
    }

    [Fact]
    public void Test3()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var logger = Mock.Of<ILogger<IotCreators>>();
        serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

        var parser = new IotCreators(serviceProviderMock.Object, null, "test");

        var rawJson = """
        {"reports":[{"value":"F86778705454507800980CD408010003076533F6CD03066533F29C013D6533E48D01416533D67C00FA6533C86903036533BA52013E6533AC4500FA65339E3200FA65339022"}]}
        """;

        var result = parser.Parse(Encoding.UTF8.GetBytes(rawJson));
        Assert.Equal("F867787054545078", result.DevEUI);
        Assert.Equal(0, result.FPort);
        Assert.Equal(638335011330000000, result.DateTime?.Ticks);
        var keyValues = result.KeyValues.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal(775m, (decimal)keyValues["distance"]);
        Assert.Equal(3.284m, (decimal)keyValues["batV"]);
        Assert.Equal(-95m, (decimal)keyValues["RSSI"]);
    }

//     [Fact]
//     public void TestDeviceStatus()
//     {
//         var serviceProviderMock = new Mock<IServiceProvider>();
//         var logger = Mock.Of<ILogger<WL03A>>();
//         serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

//         var parser = new WL03A(serviceProviderMock.Object, null, "test");

//         var rawJson = """
//         { "end_device_ids": { "dev_eui": "eui" }, "uplink_message": { "f_port": 5, "received_at": "2024-08-09T14:14:13.093129857Z", "frm_payload": "HQEAAf8OLg==", "decoded_payload": "", "rx_metadata": [ {"rssi": "-81" } ] } }
//         """;

//         var result = parser.Parse(TheThingsNetworkPacketCreator.Create(rawJson));
//         Assert.Equal("eui", result.DevEUI);
//         Assert.Equal(5, result.FPort);
//         Assert.Equal(638588096530931298, result.DateTime?.Ticks);
//         var keyValues = result.KeyValues.ToDictionary(kv => kv.Key, kv => kv.Value);        
//         Assert.Equal(3.63m, (decimal)keyValues["batV"]);
//         Assert.Equal(-81m, (decimal)keyValues["RSSI"]);
//     }

//     [Fact]
//     public void TestTTN()
//     {
//         var serviceProviderMock = new Mock<IServiceProvider>();
//         var logger = Mock.Of<ILogger<WL03A>>();
//         serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

//         var parser = new WL03A(serviceProviderMock.Object, null, "test");

//         // Copy/Paste data property of TheThingsNetwork event details log (from Live Data)
//         var ttnDataJson = """
// {
//     "@type": "type.googleapis.com/ttn.lorawan.v3.ApplicationUp",
//     "end_device_ids": {
//       "device_id": "fx-soilmoisture-1",
//       "application_ids": {
//         "application_id": "fx-dragino-se01-lb"
//       },
//       "dev_eui": "A84041C891885C21",
//       "join_eui": "A840410000000101",
//       "dev_addr": "260B1091"
//     },
//     "correlation_ids": [
//       "gs:uplink:01J4XS1S7TEM75KF1YTBVJ3JJC"
//     ],
//     "received_at": "2024-08-10T09:13:57.452142969Z",
//     "uplink_message": {
//       "session_key_id": "AZEyxQRFur+fOJACRxg1JA==",
//       "f_port": 2,
//       "f_cnt": 123,
//       "frm_payload": "DScMzA4jCX0BPhA=",
//       "decoded_payload": {
//         "BatV": 3.367,
//         "Mod": 0,
//         "conduct_SOIL": 318,
//         "i_flag": 0,
//         "s_flag": 1,
//         "temp_DS18B20": "327.60",
//         "temp_SOIL": "24.29",
//         "water_SOIL": "36.19"
//       },
//       "rx_metadata": [
//         {
//           "gateway_ids": {
//             "gateway_id": "obiwan-bu",
//             "eui": "A840411E96004150"
//           },
//           "time": "2024-08-10T09:13:57.230327Z",
//           "timestamp": 2922010691,
//           "rssi": -88,
//           "channel_rssi": -88,
//           "snr": 9.2,
//           "location": {
//             "latitude": 51.08095332226225,
//             "longitude": 3.1239420175552373,
//             "altitude": 32,
//             "source": "SOURCE_REGISTRY"
//           },
//           "uplink_token": "ChcKFQoJb2Jpd2FuLWJ1EgioQEEelgBBUBDDsKnxChoLCNXe3LUGEKeDsHMguIujq4XZzAI=",
//           "channel_index": 4,
//           "received_at": "2024-08-10T09:13:57.223653340Z"
//         }
//       ],
//       "settings": {
//         "data_rate": {
//           "lora": {
//             "bandwidth": 125000,
//             "spreading_factor": 7,
//             "coding_rate": "4/5"
//           }
//         },
//         "frequency": "867300000",
//         "timestamp": 2922010691,
//         "time": "2024-08-10T09:13:57.230327Z"
//       },
//       "received_at": "2024-08-10T09:13:57.243570802Z",
//       "consumed_airtime": "0.061696s",
//       "version_ids": {
//         "brand_id": "dragino",
//         "model_id": "lse01",
//         "hardware_version": "_unknown_hw_version_",
//         "firmware_version": "1.3",
//         "band_id": "EU_863_870"
//       },
//       "network_ids": {
//         "net_id": "000013",
//         "ns_id": "EC656E0000000181",
//         "tenant_id": "ttn",
//         "cluster_id": "eu1",
//         "cluster_address": "eu1.cloud.thethings.network"
//       }
//     }
//   }
// """;

//         var result = parser.Parse(TheThingsNetworkPacketCreator.Create(ttnDataJson));
//         Assert.Equal("A84041C891885C21", result.DevEUI);
//         Assert.True(result.DateTime.HasValue);
//         var keyValues = result.KeyValues.ToDictionary(kv => kv.Key, kv => kv.Value);
//         Assert.Equal(3.367m, (decimal)keyValues["batV"]);
//         Assert.Equal(36.19m, (decimal)keyValues["soilMoisturePrc"]);
//         Assert.Equal(24.29m, (decimal)keyValues["soilTemperature"]);
//         Assert.Equal(318m, (decimal)keyValues["soilConductivity"]);
//         Assert.Equal(-88m, (decimal)keyValues["RSSI"]);
//     }
}