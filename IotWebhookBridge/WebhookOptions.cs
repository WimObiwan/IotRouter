namespace IotWebhookBridge;

public class WebhookOptions
{
    public const string Position = "Webhook";

    //public string[] SourceIps { get; init; } = default!;
    public string? SecretHeader { get; init; }
    public string? SecretValue { get; init; }
}