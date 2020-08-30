using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace IotRouter
{
    public class MqttListener : IListener
    {
        ILogger<MqttListener> _logger;
        IMqttClient _mqttClient;
        private bool disposedValue;
        private bool disconnecting;

        public string Name { get; private set; }
        public string Server { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Topic { get; private set; }

        public event MessageReceivedHandler MessageReceived;

        public MqttListener(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetService<ILogger<MqttListener>>();
            Name = name;
            Server = config.GetValue<string>("Server");
            Username = config.GetValue<string>("Username");
            Password = config.GetValue<string>("Password");
            Topic = config.GetValue<string>("Topic");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            disconnecting = false;
            MqttFactory factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("IotRouter")
                .WithTcpServer(Server)
                .WithCredentials(Username, Password)
                .WithTls()
                .WithCleanSession()
                .Build();

            _mqttClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation($"MqttListener {Name}: Connected");
                await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(Topic).Build());
                _logger.LogInformation($"MqttListener {Name}: Subscribed");
            });

            _mqttClient.UseDisconnectedHandler(async e =>
            {
                if (!disconnecting) {
                    _logger.LogWarning($"MqttListener {Name}: Disconnected, trying to reconnect");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await _mqttClient.ConnectAsync(options, CancellationToken.None);
                }
            });

            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                _logger.LogInformation($"MqttListener {Name}: Message received\n"
                    + $"+ Topic = {e.ApplicationMessage.Topic}\n"
                    + $"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}\n"
                    + $"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}\n"
                    + $"+ Retain = {e.ApplicationMessage.Retain}");
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload));
            });

            await _mqttClient.ConnectAsync(options, cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            disconnecting = true;
            await _mqttClient.DisconnectAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _mqttClient?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}