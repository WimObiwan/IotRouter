using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace IotWebhookBridge;

public interface IMqttService
{
    Task SendAsync(string text, string suffix);
}

public class MqttService : IMqttService, IDisposable
{
    private bool _connected;
    private bool _disposedValue;
    private IMqttClient? _mqttClient;
    private MqttOptions _mqttOptions;

    public MqttService(IOptions<MqttOptions> mqttOptions)
    {
        _mqttOptions = mqttOptions.Value;
    }
    
    public async Task SendAsync(string text, string suffix)
    {
        if (!_connected || _mqttClient == null)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
                .WithTcpServer(_mqttOptions.Uri, _mqttOptions.Port)
                .WithCleanSession()
                .WithTls(o => 
                {
                    o.IgnoreCertificateRevocationErrors = true;
                    o.IgnoreCertificateChainErrors = true;
                });

            var options = messageBuilder.Build();
            await _mqttClient.ConnectAsync(options);
            _connected = true;
        }

        if (!_mqttClient.IsConnected)
        {
            await _mqttClient.ReconnectAsync();
        }

        // var mqttData = new MqttData(parsedData);
        // //_logger.LogInformation($"{Measurement}, {parsedData.DateTime}, {keyValues.Count()}");

        string topic = _mqttOptions.Topic;
        if (!string.IsNullOrEmpty(suffix))
            topic = topic + '/' + suffix;
        
        // var payload = System.Text.Json.JsonSerializer.Serialize(mqttData);
        var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(text)
                //.WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();
        await _mqttClient.PublishAsync(message);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_mqttClient != null)
                    _mqttClient.Dispose();
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