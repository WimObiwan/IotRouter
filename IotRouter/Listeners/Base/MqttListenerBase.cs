// using System;
// using System.Diagnostics;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using IotRouter.Util;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using MQTTnet;
// using MQTTnet.Client;
//
// namespace IotRouter.Listeners.Base;
//
// public abstract class MqttListenerBase : IDisposable
// {
//     private readonly ILogger<MqttListenerBase> _logger;
//     private readonly CancellationTokenSource _reconnectCancellationTokenSource = new(); 
//     IMqttClient _mqttClient;
//     private bool _disposedValue;
//     private int _trial = 0;
//     
//     protected abstract string DisplayName { get; }
//     protected abstract Task<MqttClientOptions> GetMqttOptions(CancellationToken cancellationToken);
//
//     protected abstract void OnMessageReceived(string topic, byte[] payload = null);
//     
//     protected MqttListenerBase(IServiceProvider serviceProvider)
//     {
//         _logger = serviceProvider.GetService<ILogger<MqttListenerBase>>();
//     }
//
//     public async Task Connect(CancellationToken cancellationToken)
//     {
//         try
//         {
//             await InitialConnect(cancellationToken);
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "StartAsync failed, schedule task to retry connect");
//             Reconnect(_reconnectCancellationTokenSource.Token).Forget();
//         }
//     }
//
//
//     private async Task InitialConnect(CancellationToken cancellationToken)
//     {
//         MqttFactory factory = new MqttFactory();
//         _mqttClient = factory.CreateMqttClient();
//
//         _mqttClient.ConnectedAsync += (async _ =>
//         {
//             _logger.LogInformation("MqttListener {Name}: Connected", DisplayName);
//             await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(_topic).Build(), _reconnectCancellationTokenSource.Token);
//             _logger.LogInformation("MqttListener {Name}: Subscribed", DisplayName);
//         });
//
//         _mqttClient.DisconnectedAsync += async _ =>
//         {
//             await Reconnect(_reconnectCancellationTokenSource.Token);
//         };
//
//         _mqttClient.ApplicationMessageReceivedAsync += (async e =>
//         {
//             _logger.LogInformation("MqttListener {Name}: Message received\n"
//                                    + "+ Topic = {Topic}\n"
//                                    + "+ Payload = {Payload}\n"
//                                    + "+ QoS = {Qos}\n"
//                                    + "+ Retain = {Retain}", 
//                 DisplayName, e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload),
//                 e.ApplicationMessage.QualityOfServiceLevel, e.ApplicationMessage.Retain);
//             
//             OnMessageReceived(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload);
//         });
//
//         MqttClientOptions mqttOptions = await GetMqttOptions(cancellationToken);
//         await _mqttClient.ConnectAsync(mqttOptions, cancellationToken);
//     }
//     
//     private async Task Reconnect(CancellationToken cancellationToken)
//     {
//         while (true)
//         {
//             if (_mqttClient.IsConnected)
//                 return;
//             try
//             {
//                 _logger.LogWarning("MqttListener {Name}: Disconnected, trying to reconnect", Name);
//                 await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
//                 
//                 await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
//                 return;
//             }
//             catch (OperationCanceledException)
//             {
//                 _logger.LogWarning("Connecting to Kress MQTT canceled");
//                 break;
//             }
//             catch (Exception e)
//             {
//                 if (_trial == 0)
//                     _trial = 1;
//                 else
//                     _trial *= 2;
//                 int minutes = 10 * _trial;
//                 
//                 _logger.LogWarning(e, "Connecting to Kress MQTT failed.  Waiting {Minutes} minutes", minutes);
//                 try
//                 {
//                     await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken);
//                 }
//                 catch (OperationCanceledException)
//                 {
//                     _logger.LogWarning("Connecting to Kress MQTT canceled");
//                     break;
//                 }
//             }
//         }
//     }
//
//     public async Task StopAsync(CancellationToken cancellationToken)
//     {
//         _reconnectCancellationTokenSource.Cancel();
//         await _mqttClient.DisconnectAsync();
//     }
//
//     protected virtual void Dispose(bool disposing)
//     {
//         if (!_disposedValue)
//         {
//             if (disposing)
//             {
//                 _mqttClient?.Dispose();
//             }
//
//             _disposedValue = true;
//         }
//     }
//
//     public void Dispose()
//     {
//         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
//         Dispose(disposing: true);
//         GC.SuppressFinalize(this);
//     }
// }