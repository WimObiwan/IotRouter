using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace IotRouter
{
    public class MqttData
    {
        public string DevEUI { get; private init; }
        public DateTime? DateTime { get; private init; }
        public Dictionary<string, object> Data { get; private init; }

        public MqttData(ParsedData parsedData)
        {
            DevEUI = parsedData.DevEUI;
            DateTime = parsedData.DateTime;
            Data = parsedData.KeyValues.ToDictionary(d => d.Key, d => d.Value);
        }
    }

    public class Mqtt : IDestination, IDisposable
    {
        ILogger<Mqtt> _logger;
        IMqttClient _mqttClient;
        private bool _connected;
        private bool _disposedValue;

        public string Name { get; private set; }
        public string Uri { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Topic { get; private set; }

        public Mqtt(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetService<ILogger<Mqtt>>();
            Name = name;
            Uri = config.GetValue<string>("Uri");
            Port = config.GetValue<int>("Port");
            Username = config.GetValue<string>("Username");
            Password = config.GetValue<string>("Password");
            Topic = config.GetValue<string>("Topic");
        }
        
        public async Task SendAsync(ParsedData parsedData)
        {
            if (!_connected)
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();
                var messageBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(Guid.NewGuid().ToString())
                    .WithCredentials(Username, Password)
                    .WithTcpServer(Uri, Port)
                    .WithCleanSession()
                    .WithTls();

                var options = messageBuilder.Build();
                await _mqttClient.ConnectAsync(options);
                _connected = true;
            }

            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ReconnectAsync();
            }

            var mqttData = new MqttData(parsedData);
            //_logger.LogInformation($"{Measurement}, {parsedData.DateTime}, {keyValues.Count()}");

            var payload = System.Text.Json.JsonSerializer.Serialize(mqttData);

            var message = new MqttApplicationMessageBuilder()
                    .WithTopic(Topic)
                    .WithPayload(payload)
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
}