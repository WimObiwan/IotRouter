namespace IotWebhookBridge;

public class MqttOptions
{
    public const string Position = "Mqtt";
    public string Uri { get; init; } = default!;
    public int Port { get; init; }
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string Topic { get; init; } = default!;
}