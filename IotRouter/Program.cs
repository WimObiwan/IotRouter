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

                    var listenerConfigs = GetTypeConfigs(configSection.GetSection("Listeners"));
                    var parserConfigs = GetTypeConfigs(configSection.GetSection("Parsers"));
                    var destinationConfigs = GetTypeConfigs(configSection.GetSection("Destinations"));

                    foreach (var routeConfig in configSection.GetSection("Routes").GetChildren().Select(m => m.Get<RouteConfig>()))
                    {
                        if (routeConfig.Disabled) 
                            continue;
                        string listenerName = Activate<IListener>(listenerConfigs[routeConfig.Listener.Name], routeConfig.Listener.Config, services);
                        string parserName = Activate<IParser>(parserConfigs[routeConfig.Parser.Name], routeConfig.Parser.Config, services);
                        IDictionary<string, IEnumerable<string>> deviceMapping = routeConfig.DeviceMapping
                            .Select(m => new {
                                    DevEUI = m.DevEUI,
                                    Destinations = (IEnumerable<string>)m.Destinations
                                        .Select(d => Activate<IDestination>(destinationConfigs[d.Name], d.Config, services))
                                        .ToList()
                                })
                            .ToDictionary(e => e.DevEUI, e => e.Destinations);                        

                        services.AddSingleton<IRoute, Route>(s =>
                            new Route(s.GetRequiredService<ILogger<Route>>())
                            {
                                Listener = listenerName,
                                Parser = parserName,
                                DeviceMapping = deviceMapping
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
