using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IotRouter.Util;
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
    private readonly CancellationTokenSource _reconnectCancellationTokenSource = new(); 

    public string Name { get; }
    private readonly string _server;
    private readonly string _username;
    private readonly string _password;
    private readonly string _topic;
    private readonly string _displayName;
    private MqttClientOptions _mqttOptions;

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
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        
        _mqttOptions = new MqttClientOptionsBuilder()
            .WithClientId("IotRouter")
            .WithTcpServer(_server)
            .WithCredentials(_username, _password)
            .WithTlsOptions(o => o.
                UseTls())
            .WithCleanSession()
            .Build();

        try
        {
            await StartAsyncWithRetry(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "StartAsync failed, schedule task to retry connect");
            Reconnect(_reconnectCancellationTokenSource.Token).Forget();
        }
    }


    public async Task StartAsyncWithRetry(CancellationToken cancellationToken)
    {
        MqttFactory factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ConnectedAsync += (async _ =>
        {
            _logger.LogInformation("MqttListener {Name}: Connected", _displayName);
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(_topic).Build(), _reconnectCancellationTokenSource.Token);
            _logger.LogInformation("MqttListener {Name}: Subscribed", _displayName);
        });

        _mqttClient.DisconnectedAsync += async _ =>
        {
            await Reconnect(_reconnectCancellationTokenSource.Token);
        };

        _mqttClient.ApplicationMessageReceivedAsync += (async e =>
        {
            _logger.LogInformation("MqttListener {Name}: Message received\n"
                                   + "+ Topic = {Topic}\n"
                                   + "+ Payload = {Payload}\n"
                                   + "+ QoS = {Qos}\n"
                                   + "+ Retain = {Retain}", 
                _displayName, e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment),
                e.ApplicationMessage.QualityOfServiceLevel, e.ApplicationMessage.Retain);
            
            if (MessageReceived != null)
                await MessageReceived.Invoke(this, new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.PayloadSegment.ToArray()));
        });

        await _mqttClient.ConnectAsync(_mqttOptions, cancellationToken);
    }

    private async Task Reconnect(CancellationToken cancellationToken)
    {
        int _trial = 0;
        while (true)
        {
            if (_mqttClient.IsConnected)
                return;
            try
            {
                _logger.LogWarning("MqttListener {Name}: Disconnected, trying to reconnect", Name);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                
                await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Connecting to MQTT canceled");
                break;
            }
            catch (Exception e)
            {
                if (_trial == 0)
                    _trial = 1;
                else
                    _trial *= 2;
                int minutes = 10 * _trial;
                
                _logger.LogWarning(e, "Connecting to MQTT failed.  Waiting {Minutes} minutes", minutes);
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Connecting to MQTT canceled");
                    break;
                }
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _reconnectCancellationTokenSource.Cancel();
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