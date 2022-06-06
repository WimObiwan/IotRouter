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
        private readonly IEnumerable<IProcessor> _processors;
        private readonly IEnumerable<IDestination> _destinations;
        private readonly IEnumerable<IRoute> _routes;

        public DaemonService(ILogger<DaemonService> logger, IOptions<Config> config, IEnumerable<IListener> listeners,
            IEnumerable<IParser> parsers, IEnumerable<IProcessor> processors,
            IEnumerable<IDestination> destinations, IEnumerable<IRoute> routes)
        {
            _logger = logger;
            _config = config;
            _listeners = listeners;
            _parsers = parsers;
            _processors = processors;
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
                var deviceMapping = route.DeviceMappings
                    .Select(d => new DeviceMapping()
                    {
                        DevEUI = d.Key,
                        Processor = d.Value.ProcessorName == null ? null : _processors.Single(s => s.Name == d.Value.ProcessorName),
                        Destinations = d.Value.DestinationNames.Select(s1 => _destinations.Single(s2 => s2.Name == s1)).AsEnumerable()
                    })
                    .ToDictionary(e => e.DevEUI, e => e);

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
            IDictionary<string, DeviceMapping> deviceMappings,
            string topic, byte[] data)
        {
            ParsedData parsedData;
            try
            {
                parsedData = parser.Parse(data);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Unable to process message");
                return Task.FromException(x);
            }

            _logger.LogInformation($"DateTime = {parsedData.DateTime}");
            _logger.LogInformation(string.Join(", ", parsedData.KeyValues.Select(kv => $"{kv.Key} = {kv.Value}")));

            return HandleMessage(deviceMappings, parsedData);
        }

        private Task HandleMessage(IDictionary<string, DeviceMapping> deviceMappings,
            ParsedData parsedData)
        {
            string devEUI = parsedData.DevEUI;

            if (deviceMappings.TryGetValue(devEUI, out DeviceMapping deviceMapping))
            {
                return HandleMessage(deviceMapping, parsedData);
            }
            else
            {
                _logger.LogError($"No DeviceMapping found for DevEUI={devEUI}");
                return Task.CompletedTask;
            }
        }

        private async Task HandleMessage(DeviceMapping deviceMapping,
            ParsedData parsedData)
        {
            if (deviceMapping.Processor != null)
            {
                bool continueProcessing = await deviceMapping.Processor.Process(parsedData);
                if (!continueProcessing)
                {
                    _logger.LogWarning($"Filtered by processor");
                }
            }

            await Task.WhenAll(deviceMapping.Destinations.Select(async (s) => 
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

        class DeviceMapping
        {
            public string DevEUI { get; set; }
            public IProcessor Processor { get; set; }
            public IEnumerable<IDestination> Destinations { get; set; }
        }
    }
}
