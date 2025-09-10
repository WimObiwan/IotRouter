using IotRouter.Parsers.Dragino;
using Microsoft.Extensions.Logging;
using Moq;

namespace IotRouterTests;

public class PSTest
{
    [Fact]
    public void Test()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var logger = Mock.Of<ILogger<PS>>();
        serviceProviderMock.Setup(m => m.GetService(It.IsAny<Type>())).Returns(logger);

        var parser = new PS(serviceProviderMock.Object, null, "test");

        var rawJson = """
        { "end_device_ids": { "dev_eui": "eui" }, "uplink_message": { "f_port": 2, "received_at": "2025-08-02T11:28:41.956945512Z", "frm_payload": "DigAAA/PAAAA", "decoded_payload": "", "rx_metadata": [ {"rssi": "-41" } ] } }
        """;

        var result = parser.Parse(TheThingsNetworkPacketCreator.Create(rawJson));
        Assert.Equal("eui", result.DevEUI);
        Assert.Equal(2, result.FPort);
        Assert.Equal(638897309219569455, result.DateTime?.Ticks);
        var keyValues = result.KeyValues.ToDictionary(kv => kv.Key, kv => kv.Value);        
        Assert.Equal(4.047m, (decimal)keyValues["pressure"]);
        Assert.Equal(14.6875m, (decimal)keyValues["distance"]);
        Assert.Equal(3.624m, (decimal)keyValues["batV"]);
        Assert.Equal(-41m, (decimal)keyValues["RSSI"]);
    }
}