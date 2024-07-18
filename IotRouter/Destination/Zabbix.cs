using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IotRouter.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZabbixSender.Async;

namespace IotRouter;

public class Zabbix : IDestination
{
    private readonly ILogger<Zabbix> _logger;

    public string Name { get; }
    private readonly string _server;
    private readonly int _port;
    private readonly string _host;

    public Zabbix(IServiceProvider serviceProvider, IConfigurationSection config, string name)
    {
        _logger = serviceProvider.GetService<ILogger<Zabbix>>();
        Name = name;
        _server = config.GetValue<string>("Server");
        _port = config.GetValue("Port", 10051);
        _host = config.GetValue<string>("Host");
    }
        
    private string GetValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    public async Task SendAsync(ParsedData parsedData)
    {
        var keyValues = parsedData.KeyValues;
        string host2 = StringPlaceholderReplacer.Replace(_host, parsedData);
        _logger.LogInformation("Host = {Host}/{Host2}, DateTime = {DateTime}, Data = {Data}",
            _host, host2, parsedData.DateTime, keyValues.Count);

        var sendData = 
            keyValues.Select(keyValue => 
                new SendData
                {
                    Host = host2,
                    Clock = parsedData.DateTime,
                    Key = keyValue.Key,
                    Value = GetValue(keyValue.Value),
                }).ToArray();
        var log = string.Join(", ", sendData.Select(s => $"{s.Key}={s.Value}"));
        _logger.LogInformation("Data = {Data}", log);

        Sender sender = new Sender(_server, _port);
        var response = await sender.Send(sendData);
        var parsedResponse = response.ParseInfo();

        _logger.LogInformation("Success = {Success}, Info = {Info}, Failed = {Failed}", 
            response.IsSuccess, response.Info, parsedResponse.Failed);
        if (parsedResponse.Processed == 0)
        {
            throw new Exception($"No Zabbix items were successfully processed ({response.Info})");
        }
        if (parsedResponse.Failed > 0)
        {
            throw new Exception($"Not all Zabbix items were successfully processed ({response.Info})");
        }
    }
}