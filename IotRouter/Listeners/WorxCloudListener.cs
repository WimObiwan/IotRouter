using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace IotRouter;

public class WorxCloudListener : IListener
{
    private readonly Uri _apiUrl;
    private readonly string _clientId;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MqttListener> _logger;
    private readonly Uri _loginUrl;
    private readonly string _password;
    private readonly string _username;
    private bool _disconnecting;
    private bool _disposedValue;

    private IMqttClient _mqttClient;

    public WorxCloudListener(IServiceProvider serviceProvider, IConfigurationSection config, string name)
    {
        _logger = serviceProvider.GetService<ILogger<MqttListener>>();
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        Name = name;
        _username = config.GetValue<string>("Username");
        _password = config.GetValue<string>("Password");

        // Kress
        _loginUrl = new Uri("https://id.kress.com/");
        _apiUrl = new Uri("https://api.kress-robotik.com/api/v2/");
        _clientId = "931d4bc4-3192-405a-be78-98e43486dc59";
    }

    public string Name { get; }

    public event MessageReceivedHandler MessageReceived;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await StartAsyncWithRetry(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "StartAsync failed");
        }
    }

    public async Task StartAsyncWithRetry(CancellationToken cancellationToken)
    {
        var mowerMqttInfo = await GetMowerInfo();

        _disconnecting = false;
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ConnectedAsync += async _ =>
        {
            _logger.LogInformation("MqttListener {Name}: Connected", Name);
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mowerMqttInfo.Topic).Build());
            _logger.LogInformation("MqttListener {Name}: Subscribed", Name);
        };

        _mqttClient.DisconnectedAsync += async _ =>
        {
            while (!_disconnecting)
                try
                {
                    _logger.LogWarning("MqttListener {Name}: Disconnected, trying to reconnect", Name);
                    await Wait(TimeSpan.FromSeconds(5));

                    var mowerMqttInfo2 = await GetMowerInfo();

                    await _mqttClient.ConnectAsync(GetMqttOptions(mowerMqttInfo2), CancellationToken.None);
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Connecting to Kress MQTT failed.  Waiting 10 minutes");
                    await Wait(TimeSpan.FromMinutes(10));
                }
        };


        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            _logger.LogInformation("MqttListener {Name}: Message received\n"
                                   + "+ Topic = {Topic}\n"
                                   + "+ Payload = {Payload}\n"
                                   + "+ QoS = {Qos}\n"
                                   + "+ Retain = {Retain}",
                Name, e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload),
                e.ApplicationMessage.QualityOfServiceLevel, e.ApplicationMessage.Retain);

            if (MessageReceived != null)
                await MessageReceived.Invoke(this,
                    new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload));
        };

        await _mqttClient.ConnectAsync(GetMqttOptions(mowerMqttInfo), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _disconnecting = true;
        await _mqttClient.DisconnectAsync();
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async Task Wait(TimeSpan duration)
    {
        var slice = TimeSpan.FromSeconds(1);
        var stopwatch = Stopwatch.StartNew();
        while (!_disconnecting)
        {
            var waitDuration = duration - stopwatch.Elapsed;
            if (waitDuration <= TimeSpan.Zero)
                return;
            if (waitDuration > slice)
                waitDuration = slice;
            await Task.Delay(waitDuration);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing) _mqttClient?.Dispose();

            _disposedValue = true;
        }
    }

    private async Task<string> LogInToWorxCloud()
    {
        var httpClient = _httpClientFactory.CreateClient();

        // HttpRequestMessage request = new(HttpMethod.Post, new Uri(_loginUrl, "oauth/token"));
        // request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-de"));

        var result = await httpClient.PostAsJsonAsync(
            new Uri(_loginUrl, "oauth/token"), new
            {
                client_id = _clientId,
                username = _username,
                password = _password,
                scope = "*",
                grant_type = "password"
            });
        result.EnsureSuccessStatusCode();

        var resultBody = await result.Content.ReadFromJsonAsync<TokenResult>();
        return resultBody.access_token;
    }

    private async Task<MowerMqttInfo> GetMowerInfo()
    {
        var accessToken = await LogInToWorxCloud();

        // $url = "https://$apiUrl/api/v2/product-items?status=1&gps_status=1"
        // $url = "https://$apiUrl/api/v2/product-items/$sn/?status=1&gps_status=1"

        var httpClient = _httpClientFactory.CreateClient();

        HttpRequestMessage request = new(HttpMethod.Get, new Uri(_apiUrl, "product-items?status=1&gps_status=1"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-de"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var result = await httpClient.SendAsync(request);
        result.EnsureSuccessStatusCode();

        var mowers = await result.Content.ReadFromJsonAsync<MowerResult[]>();
        var mower = mowers[0];
        var uuid = mower.uuid;
        var mqttEndpoint = mower.mqtt_endpoint;
        var topic = mower.mqtt_topics.command_out;
        //string region = "eu-west-1"

        var brandPrefix = "KR";
        var userId = mower.user_id;
        var clientId = $"{brandPrefix}/USER/{userId}/bot/{uuid}";

        var accessTokenParts = accessToken
            .Replace('_', '/')
            .Replace('-', '+')
            .Split('.');
        var coll = HttpUtility.ParseQueryString("");
        coll.Add("jwt", $"{accessTokenParts[0]}.{accessTokenParts[1]}");
        coll.Add("x-amz-customauthorizer-name", ""); // 'com-worxlandroid-customer'
        coll.Add("x-amz-customauthorizer-signature", accessTokenParts[2]);
        var username = "bot?" + coll;

        return new MowerMqttInfo
        {
            ClientId = clientId,
            UserName = username,
            Password = "",
            Topic = topic,
            Endpoint = mqttEndpoint
        };
    }

    private MqttClientOptions GetMqttOptions(MowerMqttInfo mowerMqttInfo)
    {
        var path = Path.Join(
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName),
            "./Data/AmazonRootCA1.pem");
        var caCrt = new X509Certificate2(File.ReadAllBytes(path));

        var options = new MqttClientOptionsBuilder()
            .WithClientId(mowerMqttInfo.ClientId)
            .WithTcpServer(mowerMqttInfo.Endpoint, 443)
            .WithCredentials(mowerMqttInfo.UserName, mowerMqttInfo.Password)
            .WithTls(
                new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    ApplicationProtocols = new List<SslApplicationProtocol> { new("mqtt") },
                    CertificateValidationHandler = certContext =>
                    {
                        var chain = new X509Chain();
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                        chain.ChainPolicy.VerificationTime = DateTime.Now;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0);
                        chain.ChainPolicy.CustomTrustStore.Add(caCrt);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                        // convert provided X509Certificate to X509Certificate2
                        var x5092 = new X509Certificate2(certContext.Certificate);

                        return chain.Build(x5092);
                    }
                }
            )
            .WithCleanSession()
            .Build();

        return options;
    }

    private class TokenResult
    {
        public int expires_in { get; init; }
        public string access_token { get; init; }
    }

    private class MqttTopics
    {
        public string command_out { get; init; }
    }

    private class MowerResult
    {
        public string uuid { get; init; }
        public string serial_number { get; init; }
        public string mqtt_endpoint { get; init; }
        public MqttTopics mqtt_topics { get; init; }
        public int user_id { get; init; }
    }

    private class MowerMqttInfo
    {
        public string ClientId { get; init; }
        public string Topic { get; init; }
        public string UserName { get; init; }
        public string Password { get; init; }
        public string Endpoint { get; init; }
    }
}