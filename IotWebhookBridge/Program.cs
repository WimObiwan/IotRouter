using IotWebhookBridge;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", true);

builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection(MqttOptions.Position));
builder.Services.AddOptions<WebhookOptions>()
    .Bind(builder.Configuration.GetSection(WebhookOptions.Position));
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();