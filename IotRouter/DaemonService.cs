using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter
{
    public class DaemonService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IOptions<Config> _config;
        private readonly IEnumerable<IListener> _listeners;
        private readonly IEnumerable<IParser> _parsers;
        private readonly IEnumerable<IDestination> _destinations;
        private readonly IEnumerable<IRoute> _routes;

        public DaemonService(ILogger<DaemonService> logger, IOptions<Config> config, IEnumerable<IListener> listeners,
            IEnumerable<IParser> parsers, IEnumerable<IDestination> destinations, IEnumerable<IRoute> routes)
        {
            _logger = logger;
            _config = config;
            _listeners = listeners;
            _parsers = parsers;
            _destinations = destinations;
            _routes = routes;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting daemon");

            // Sanity checks
            AssertUniqueNames<IListener>(_listeners, l => l.Name);
            AssertUniqueNames<IParser>(_parsers, p => p.Name);
            AssertUniqueNames<IDestination>(_destinations, s => s.Name);

            // Wire Routes
            foreach (var route in _routes)
            {
                var listener = _listeners.Single(l => l.Name == route.Listener);
                var parser = _parsers.Single(p => p.Name == route.Parser);
                // var destinations = route.Destinations.Select(s1 => _destinations.Single(s2 => s2.Name == s1)).AsEnumerable();
                var deviceMapping = route.DeviceMapping
                    .Select(d => new 
                    {
                        DevEUI = d.Key,
                        Destinations = d.Value.Select(s1 => _destinations.Single(s2 => s2.Name == s1)).AsEnumerable()
                    })
                    .ToDictionary(e => e.DevEUI, e => e.Destinations);

                listener.MessageReceived += (s, e) => ListenerMessageReceived(listener, parser, deviceMapping, e.Topic, e.Payload);
            }

            await Task.WhenAll(_listeners.Select(l => l.StartAsync(cancellationToken)));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping daemon.");

            await Task.WhenAll(_listeners.Select(l => l.StopAsync(cancellationToken)));
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing....");
        }

        private void AssertUniqueNames<T>(IEnumerable<T> collection, Func<T, string> nameSelector)
        {
            var nonUniqueNames = collection.GroupBy(nameSelector).Where(g => g.Count() > 1).Select(g => g.Key);
            if (nonUniqueNames.Any())
            {
                throw new Exception($"Names in {typeof(T).Name} are not unique ({string.Join(", ", nonUniqueNames)})"); 
            }
        }

        private Task ListenerMessageReceived(IListener listener, IParser parser, 
            IDictionary<string, IEnumerable<IDestination>> deviceMapping,
            string topic, byte[] data)
        {
            ParsedData parsedData;
            try
            {
                parsedData = parser.Parse(data);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Unable te process message");
                return Task.FromException(x);
            }

            _logger.LogInformation($"DateTime = {parsedData.DateTime}");
            _logger.LogInformation(string.Join(", ", parsedData.KeyValues.Select(kv => $"{kv.Key} = {kv.Value}")));

            return HandleMessage (deviceMapping, parsedData);
        }

        private Task HandleMessage(IDictionary<string, IEnumerable<IDestination>> deviceMapping,
            ParsedData parsedData)
        {
            string devEUI = parsedData.DevEUI;

            if (deviceMapping.TryGetValue(devEUI, out IEnumerable<IDestination> destinations))
            {
                return HandleMessage(destinations, parsedData);
            }
            else
            {
                _logger.LogError($"No DeviceMapping found for DevEUI={devEUI}");
                return Task.CompletedTask;
            }
        }

        private Task HandleMessage(IEnumerable<IDestination> destinations,
            ParsedData parsedData)
        {
            return Task.WhenAll(destinations.Select(async (s) => 
            {
                try
                {
                    await s.SendAsync(parsedData);
                }
                catch (Exception x)
                {
                    _logger.LogError(x, $"Destination {s.Name} failed");
                    throw;
                }
            }));
        }
    }
}
