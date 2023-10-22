namespace IotWebhookBridge;

public class UdpServerOptions
{
    public const string Position = "UdpServer";
    public int Port { get; init; }
    public string[] IpWhitelist { get; init; } = null!;
}