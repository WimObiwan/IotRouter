using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public class InfluxDB : IDestination
    {
        ILogger<InfluxDB> _logger;

        public string Name { get; private set; }
        public string Server { get; private set; }
        public int Port { get; private set; }
        public string Host { get; private set; }

        public InfluxDB(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetService<ILogger<InfluxDB>>();
            Name = name;
            Server = config.GetValue<string>("Server");
            Port = config.GetValue("Port", 10051);
            Host = config.GetValue<string>("Host");
        }
        
        private string GetValue(object value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public async Task SendAsync(ParsedData parsedData)
        {
            // var keyValues = parsedData.KeyValues;
            // _logger.LogInformation($"{Host}, {parsedData.DateTime}, {keyValues.Count()}");

            // var sendData = 
            //     keyValues.Select(keyValue => 
            //         new SendData()
            //             {
            //                 Host = Host,
            //                 Clock = parsedData.DateTime,
            //                 Key = keyValue.Key,
            //                 Value = GetValue(keyValue.Value),
            //             }).ToArray();
            // var log = string.Join(", ", sendData.Select(s => $"{s.Key}={s.Value}"));
            // _logger.LogInformation(log);

            // Sender sender = new Sender(Server, Port);
            // var response = await sender.Send(sendData);
            // var parsedResponse = response.ParseInfo();

            // _logger.LogInformation($"{response.IsSuccess} {response.Info} {parsedResponse.Failed}");
            // if (parsedResponse.Processed == 0)
            // {
            //     throw new Exception($"No Zabbix items were succesfully processed ({response.Info})");
            // }
            // else if (parsedResponse.Failed > 0)
            // {
            //     throw new Exception($"Not all Zabbix items were succesfully processed ({response.Info})");
            // }
        }
    }
}