using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace IotRouter;

public class MqttListener : IListener
{
    private readonly ILogger<MqttListener> _logger;
    IMqttClient _mqttClient;
    private bool _disposedValue;
    private bool _disconnecting;

    public string Name { get; }
    private readonly string _server;
    private readonly string _username;
    private readonly string _password;
    private readonly string _topic;
    private readonly string _displayName;

    public event MessageReceivedHandler MessageReceived;

    public MqttListener(IServiceProvider serviceProvider, IConfigurationSection config, string name)
    {
        _logger = serviceProvider.GetService<ILogger<MqttListener>>();
        Name = name;
        _server = config.GetValue<string>("Server");
        _username = config.GetValue<string>("Username");
        _password = config.GetValue<string>("Password");
        _topic = config.GetValue<string>("Topic");
        _displayName = $"{_server}-{_username}";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _disconnecting = false;
        MqttFactory factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId("IotRouter")
            .WithTcpServer(_server)
            .WithCredentials(_username, _password)
            .WithTls()
            .WithCleanSession()
            .Build();

        _mqttClient.ConnectedAsync += (async _ =>
        {
            _logger.LogInformation("MqttListener {Name}: Connected", _displayName);
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(_topic).Build());
            _logger.LogInformation("MqttListener {Name}: Subscribed", _displayName);
        });

        _mqttClient.DisconnectedAsync += (async _ =>
        {
            if (!_disconnecting) {
                _logger.LogWarning("MqttListener {Name}: Disconnected, trying to reconnect", _displayName);
                await Task.Delay(TimeSpan.FromSeconds(5));
                await _mqttClient.ConnectAsync(options, CancellationToken.None);
            }
        });

        _mqttClient.ApplicationMessageReceivedAsync += (async e =>
        {
            _logger.LogInformation("MqttListener {Name}: Message received\n"
                                   + "+ Topic = {Topic}\n"
                                   + "+ Payload = {Payload}\n"
                                   + "+ QoS = {Qos}\n"
                                   + "+ Retain = {Retain}", 
                _displayName, e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload),
                e.ApplicationMessage.QualityOfServiceLevel, e.ApplicationMessage.Retain);
            
            if (MessageReceived != null)
                await MessageReceived.Invoke(this, new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload));
        });

        await _mqttClient.ConnectAsync(options, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _disconnecting = true;
        await _mqttClient.DisconnectAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _mqttClient?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}