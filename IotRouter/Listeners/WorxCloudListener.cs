// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Net.Http;
// using System.Net.Http.Headers;
// using System.Net.Http.Json;
// using System.Net.Security;
// using System.Security.Authentication;
// using System.Security.Cryptography.X509Certificates;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Web;
// using IotRouter.Util;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using MQTTnet;
// using MQTTnet.Client;

// namespace IotRouter;

// public class WorxCloudListener : IListener
// {
//     private readonly Uri _apiUrl;
//     private readonly string _clientId;
//     private readonly IHttpClientFactory _httpClientFactory;
//     private readonly ILogger<MqttListener> _logger;
//     private readonly Uri _loginUrl;
//     private readonly string _password;
//     private readonly string _username;
//     private readonly CancellationTokenSource _reconnectCancellationTokenSource = new(); 
//     private bool _disposedValue;
//     private int _trial = 0;

//     private IMqttClient _mqttClient;

//     public WorxCloudListener(IServiceProvider serviceProvider, IConfigurationSection config, string name)
//     {
//         _logger = serviceProvider.GetService<ILogger<MqttListener>>();
//         _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
//         Name = name;
//         _username = config.GetValue<string>("Username");
//         _password = config.GetValue<string>("Password");

//         // Kress
//         _loginUrl = new Uri("https://id.kress.com/");
//         _apiUrl = new Uri("https://api.kress-robotik.com/api/v2/");
//         _clientId = "931d4bc4-3192-405a-be78-98e43486dc59";
//     }

//     public string Name { get; }

//     public event MessageReceivedHandler MessageReceived;

//     public async Task StartAsync(CancellationToken cancellationToken)
//     {
//         var factory = new MqttFactory();
//         _mqttClient = factory.CreateMqttClient();

//         try
//         {
//             await StartAsyncWithRetry(cancellationToken);
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "StartAsync failed, schedule task to retry connect");
//             Reconnect(_reconnectCancellationTokenSource.Token).Forget();
//         }
//     }

//     public async Task StartAsyncWithRetry(CancellationToken cancellationToken)
//     {
//         var mowerMqttInfo = await GetMowerInfo(cancellationToken);
        
//         _mqttClient.ConnectedAsync += async _ =>
//         {
//             _logger.LogInformation("MqttListener {Name}: Connected", Name);
//             await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mowerMqttInfo.Topic).Build(), _reconnectCancellationTokenSource.Token);
//             _logger.LogInformation("MqttListener {Name}: Subscribed", Name);
//             _trial = 0;
//         };

//         _mqttClient.DisconnectedAsync += async _ =>
//         {
//             await Reconnect(_reconnectCancellationTokenSource.Token);
//         };


//         _mqttClient.ApplicationMessageReceivedAsync += async e =>
//         {
//             _logger.LogInformation("MqttListener {Name}: Message received\n"
//                                    + "+ Topic = {Topic}\n"
//                                    + "+ Payload = {Payload}\n"
//                                    + "+ QoS = {Qos}\n"
//                                    + "+ Retain = {Retain}",
//                 Name, e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment),
//                 e.ApplicationMessage.QualityOfServiceLevel, e.ApplicationMessage.Retain);

//             if (MessageReceived != null)
//                 await MessageReceived.Invoke(this,
//                     new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.PayloadSegment.ToArray()));
//         };

//         await _mqttClient.ConnectAsync(GetMqttOptions(mowerMqttInfo), cancellationToken);
//     }

//     private async Task Reconnect(CancellationToken cancellationToken)
//     {
//         while (true)
//         {
//             try
//             {
//                 _logger.LogWarning("MqttListener {Name}: Disconnected, trying to reconnect", Name);
//                 await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

//                 var mowerMqttInfo2 = await GetMowerInfo(cancellationToken);

//                 await _mqttClient.ConnectAsync(GetMqttOptions(mowerMqttInfo2), cancellationToken);
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

//     public async Task StopAsync(CancellationToken cancellationToken)
//     {
//         _reconnectCancellationTokenSource.Cancel();
//         await _mqttClient.DisconnectAsync();
//     }

//     public void Dispose()
//     {
//         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
//         Dispose(true);
//         GC.SuppressFinalize(this);
//     }

//     protected virtual void Dispose(bool disposing)
//     {
//         if (!_disposedValue)
//         {
//             if (disposing) _mqttClient?.Dispose();

//             _disposedValue = true;
//         }
//     }

//     private async Task<string> LogInToWorxCloud(CancellationToken cancellationToken)
//     {
//         var httpClient = _httpClientFactory.CreateClient();

//         // HttpRequestMessage request = new(HttpMethod.Post, new Uri(_loginUrl, "oauth/token"));
//         // request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//         // request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-de"));

//         var result = await httpClient.PostAsJsonAsync(
//             new Uri(_loginUrl, "oauth/token"), new
//             {
//                 client_id = _clientId,
//                 username = _username,
//                 password = _password,
//                 scope = "*",
//                 grant_type = "password"
//             }, cancellationToken);
//         try
//         {
//             result.EnsureSuccessStatusCode();
//         }
//         catch
//         {
//             string headers = string.Join("; ", 
//                 result.Headers.SelectMany(h => h.Value.Select(v => $"{h.Key}={v}")));
//             string content = await result.Content.ReadAsStringAsync(cancellationToken);
//             _logger.LogError("Kress login failed: {StatusCode} - {ReasonPhrase}\n{Headers}\n{Content}", (int)result.StatusCode,
//                 result.ReasonPhrase, headers, content);
//             throw;
//         }

//         var resultBody = await result.Content.ReadFromJsonAsync<TokenResult>(cancellationToken: cancellationToken);
//         return resultBody.access_token;
//     }

//     private async Task<MowerMqttInfo> GetMowerInfo(CancellationToken cancellationToken)
//     {
//         var accessToken = await LogInToWorxCloud(cancellationToken);

//         // $url = "https://$apiUrl/api/v2/product-items?status=1&gps_status=1"
//         // $url = "https://$apiUrl/api/v2/product-items/$sn/?status=1&gps_status=1"

//         var httpClient = _httpClientFactory.CreateClient();

//         HttpRequestMessage request = new(HttpMethod.Get, new Uri(_apiUrl, "product-items?status=1&gps_status=1"));
//         request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//         request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-de"));
//         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

//         var result = await httpClient.SendAsync(request, cancellationToken);
//         result.EnsureSuccessStatusCode();

//         var mowers = await result.Content.ReadFromJsonAsync<MowerResult[]>(cancellationToken: cancellationToken);
//         var mower = mowers[0];
//         var uuid = mower.uuid;
//         var mqttEndpoint = mower.mqtt_endpoint;
//         var topic = mower.mqtt_topics.command_out;
//         //string region = "eu-west-1"

//         var brandPrefix = "KR";
//         var userId = mower.user_id;
//         var clientId = $"{brandPrefix}/USER/{userId}/bot/{uuid}";

//         var accessTokenParts = accessToken
//             .Replace('_', '/')
//             .Replace('-', '+')
//             .Split('.');
//         var coll = HttpUtility.ParseQueryString("");
//         coll.Add("jwt", $"{accessTokenParts[0]}.{accessTokenParts[1]}");
//         coll.Add("x-amz-customauthorizer-name", ""); // 'com-worxlandroid-customer'
//         coll.Add("x-amz-customauthorizer-signature", accessTokenParts[2]);
//         var username = "bot?" + coll;

//         return new MowerMqttInfo
//         {
//             ClientId = clientId,
//             UserName = username,
//             Password = "",
//             Topic = topic,
//             Endpoint = mqttEndpoint
//         };
//     }

//     private MqttClientOptions GetMqttOptions(MowerMqttInfo mowerMqttInfo)
//     {
//         var path = Path.Join(
//             Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName),
//             "./Data/AmazonRootCA1.pem");
//         var caCrt = new X509Certificate2(File.ReadAllBytes(path));

//         var options = new MqttClientOptionsBuilder()
//             .WithClientId(mowerMqttInfo.ClientId)
//             .WithTcpServer(mowerMqttInfo.Endpoint, 443)
//             .WithCredentials(mowerMqttInfo.UserName, mowerMqttInfo.Password)
//             .WithTlsOptions(o => o
//                 .UseTls()
//                 .WithSslProtocols(SslProtocols.Tls12)
//                 .WithApplicationProtocols(new List<SslApplicationProtocol> { new("mqtt") })
//                 .WithCertificateValidationHandler(certContext =>
//                     {
//                         var chain = new X509Chain();
//                         chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
//                         chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
//                         chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
//                         chain.ChainPolicy.VerificationTime = DateTime.Now;
//                         chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);
//                         chain.ChainPolicy.CustomTrustStore.Add(caCrt);
//                         chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

//                         // convert provided X509Certificate to X509Certificate2
//                         var x5092 = new X509Certificate2(certContext.Certificate);

//                         return chain.Build(x5092);
//                     }
//                 )
//             )
//             .WithCleanSession()
//             .Build();

//         return options;
//     }

//     private class TokenResult
//     {
//         public int expires_in { get; init; }
//         public string access_token { get; init; }
//     }

//     private class MqttTopics
//     {
//         public string command_out { get; init; }
//     }

//     private class MowerResult
//     {
//         public string uuid { get; init; }
//         public string serial_number { get; init; }
//         public string mqtt_endpoint { get; init; }
//         public MqttTopics mqtt_topics { get; init; }
//         public int user_id { get; init; }
//     }

//     private class MowerMqttInfo
//     {
//         public string ClientId { get; init; }
//         public string Topic { get; init; }
//         public string UserName { get; init; }
//         public string Password { get; init; }
//         public string Endpoint { get; init; }
//     }
// }