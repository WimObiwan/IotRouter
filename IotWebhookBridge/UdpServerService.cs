
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace IotWebhookBridge;

public interface IUdpServerService
{
    void Initialize();
}

public class UdpServerService : IUdpServerService, IAsyncDisposable
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<UdpServerService> _logger;
    private readonly UdpServerOptions _options;
    private Tuple<Task, CancellationTokenSource>? _udpServerTask;

    public UdpServerService(IMqttService mqttService, IOptions<UdpServerOptions> options, ILogger<UdpServerService> logger)
    {
        _mqttService = mqttService;
        _logger = logger;
        _options = options.Value;
    }
    
    public void Initialize()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var ipNetworkWhitelist = _options.IpWhitelist.Select(ParseIpNetwork).ToList();

        var task = Task.Run(() => ThreadMethod(cancellationTokenSource.Token, ipNetworkWhitelist), cancellationTokenSource.Token);
        _udpServerTask = new Tuple<Task, CancellationTokenSource>(task, cancellationTokenSource);
    }

    private IPNetwork ParseIpNetwork(string s)
    {
        int slash = s.IndexOf('/');
        IPAddress ip;
        int prefixLength;
        if (slash >= 0)
        {
            ip = IPAddress.Parse(s.Substring(0, slash));
            prefixLength = int.Parse(s.Substring(slash + 1));
        }
        else
        {
            ip = IPAddress.Parse(s);
            prefixLength = 32;
        }

        return new IPNetwork(ip, prefixLength);
    }

    private async Task ThreadMethod(CancellationToken ct, List<IPNetwork> ipWhitelistNetworks)
    {
        try
        {
            int port = _options.Port;
            using var udpClient = new UdpClient(port);
            while (!ct.IsCancellationRequested)
            {
                var receive = await udpClient.ReceiveAsync(ct);
                _logger.LogInformation("Udp packet received from {RemoteIp}, DataLength {DataLength}",
                    receive.RemoteEndPoint.Address, receive.Buffer.Length);
                if (!ipWhitelistNetworks.Any(n => n.Contains(receive.RemoteEndPoint.Address)))
                {
                    _logger.LogWarning("Udp packet from {RemoteIp} rejected by whitelist",
                        receive.RemoteEndPoint.Address);
                    continue;
                }
                
                var packet = new Packet
                {
                    reports = new[]
                    {
                        new Packet.Report
                        {
                            value = Convert.ToHexString(receive.Buffer)
                        }
                    }
                };

                await _mqttService.SendAsync(JsonSerializer.Serialize(packet), $"port-{port}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_udpServerTask != null)
        {
            var task = _udpServerTask.Item1;
            var cancellationTokenSource = _udpServerTask.Item2;
            cancellationTokenSource.Cancel();
            try
            {
                // ReSharper disable once MethodSupportsCancellation
                await task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception)
            {
                // ignored
            }

            task.Dispose();
            cancellationTokenSource.Dispose();
            _udpServerTask = null;
        }
    }
    
    private class Packet
    {
        public class Report
        {
            //public string serialNumber { get; init; }
            //public long timestamp { get; init; }
            public required string value { get; init; }
        }

        public required Report[] reports { get; init; }
    }
}