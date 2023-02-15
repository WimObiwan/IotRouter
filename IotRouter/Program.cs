using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json");
                    config.AddJsonFile("appsettings.Local.json", true);

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    var configSection = hostContext.Configuration.GetSection("Config");
                    services.Configure<Config>(configSection);
                    var sect = hostContext.Configuration.GetSection("Config").GetSection("Listeners").GetChildren();

                    services.AddSingleton<IHostedService, DaemonService>();
                    services.AddSingleton<IStateService, StateService>();

                    var listenerConfigs = GetTypeConfigs(configSection.GetSection("Listeners"));
                    var parserConfigs = GetTypeConfigs(configSection.GetSection("Parsers"));
                    var processorConfigs = GetTypeConfigs(configSection.GetSection("Processors"));
                    var destinationConfigs = GetTypeConfigs(configSection.GetSection("Destinations"));

                    foreach (var routeConfig in configSection.GetSection("Routes").GetChildren().Select(m => m.Get<RouteConfig>()))
                    {
                        if (routeConfig.Disabled) 
                            continue;
                        string listenerName = Activate<IListener>(listenerConfigs[routeConfig.Listener.Name], routeConfig.Listener.Config, services);
                        string parserName = Activate<IParser>(parserConfigs[routeConfig.Parser.Name], routeConfig.Parser.Config, services);
                        IDictionary<string, DeviceMapping> deviceMappings = routeConfig.DeviceMapping
                            .Select(m => new DeviceMapping() 
                                {
                                    DevEUI = m.DevEUI,
                                    ProcessorName = 
                                        m.Processor == null ? 
                                            null 
                                            : Activate<IProcessor>(processorConfigs[m.Processor.Name], m.Processor.Config, services),
                                    DestinationNames = m.Destinations
                                        .Select(d => Activate<IDestination>(destinationConfigs[d.Name], d.Config, services))
                                        .ToList()
                                })
                            .ToDictionary(e => e.DevEUI, e => e);                        

                        services.AddSingleton<IRoute, Route>(s =>
                            new Route(s.GetRequiredService<ILogger<Route>>())
                            {
                                Listener = listenerName,
                                Parser = parserName,
                                DeviceMappings = deviceMappings
                            });
                    }
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            await builder.RunConsoleAsync();
        }

        private static Dictionary<string, TypeConfig> GetTypeConfigs(IConfiguration configSection)
        {
            return 
                configSection
                    .GetChildren()
                    .Select(s => s.Get<TypeConfig>())
                    .ToDictionary(s => s.Name);
        }

        private static string Activate<IService>(TypeConfig typeConfig, IConfigurationSection instanceConfig,
            IServiceCollection services)
            where IService : class
        {
            string name = Guid.NewGuid().ToString();
            
            IConfigurationSection mergedConfig = new MergedConfigurationSection(instanceConfig, typeConfig.Config);            
            Type type = Type.GetType(typeConfig.Type);
            services.AddSingleton<IService>(s =>
                (IService)Activator.CreateInstance(type, 
                    s.GetRequiredService<IServiceProvider>(),
                    mergedConfig,
                    name));
            return name;
        }
    }
}
