using IotWebhookBridge;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", true);

builder.Services.AddOptions<MqttOptions>()
    .Bind(builder.Configuration.GetSection(MqttOptions.Position));
builder.Services.AddOptions<WebhookOptions>()
    .Bind(builder.Configuration.GetSection(WebhookOptions.Position));
builder.Services.AddOptions<UdpServerOptions>()
    .Bind(builder.Configuration.GetSection(UdpServerOptions.Position));

builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<IUdpServerService, UdpServerService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Services.GetRequiredService<IUdpServerService>().Initialize();

app.Run();