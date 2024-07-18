using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vibrant.InfluxDB.Client;
using Vibrant.InfluxDB.Client.Rows;

namespace IotRouter
{
    public class InfluxDB : IDestination, IDisposable
    {
        ILogger<InfluxDB> _logger;
        private bool disposedValue;

        InfluxClient InfluxClient;

        public string Name { get; private set; }
        public string Database { get; private set; }
        public string Measurement { get; private set; }

        public InfluxDB(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        {
            _logger = serviceProvider.GetService<ILogger<InfluxDB>>();
            Name = name;
            string url = config.GetValue<string>("Url");
            string username = config.GetValue<string>("Username");
            string password = config.GetValue<string>("Password");
            Database = config.GetValue<string>("Database");
            //string retentionPolicy = config.GetValue<string>("RetentionPolicy");
            Measurement = config.GetValue<string>("Measurement");

            // var influxDBClientOptions = InfluxDBClientOptions.Builder.CreateNew()
            //     .Url(url)
            //     .Authenticate(username, password.ToCharArray())
            //     .Org("-")
            //     .Bucket(database)
            //     .Build();
            // InfluxDBClient = InfluxDBClientFactory.Create(influxDBClientOptions);
            // InfluxDBClient = InfluxDBClientFactory.CreateV1(url, username, password.ToCharArray(), database, retentionPolicy);
            // WriteApi = InfluxDBClient.GetWriteApi();

            InfluxClient = new InfluxClient(new Uri(url), username, password);
        }
        
        public async Task SendAsync(ParsedData parsedData)
        {
            // TODO: Add to config!
            var filter = new string[] { "BatV", "RSSI", "distance" };
            
            var keyValues = parsedData.KeyValues.Where(kv => filter.Contains(kv.Key));
            _logger.LogInformation($"{Measurement}, {parsedData.DateTime}, {keyValues.Count()}");

            // var point = PointData.Measurement(Measurement);
            // if (parsedData.DateTime.HasValue)
            //     point = point.Timestamp(parsedData.DateTime.Value, WritePrecision.Ms);
            // if (!string.IsNullOrEmpty(parsedData.DevEUI))
            //     point = point.Tag("DevEUI", parsedData.DevEUI);
            // foreach (var keyValue in parsedData.KeyValues) 
            // {
            //     if (keyValue.Value is int valInt)
            //         point = point.Field(keyValue.Key, (long)valInt);
            //     else if (keyValue.Value is long valLong)
            //         point = point.Field(keyValue.Key, valLong);
            //     else if (keyValue.Value is ulong valUlong)
            //         point = point.Field(keyValue.Key, valUlong);
            //     else if (keyValue.Value is double valDouble)
            //         point = point.Field(keyValue.Key, valDouble);
            //     else if (keyValue.Value is float valFloat)
            //         point = point.Field(keyValue.Key, valFloat);
            //     else if (keyValue.Value is decimal valDecimal)
            //         point = point.Field(keyValue.Key, valDecimal);
            //     else if (keyValue.Value is bool valBool)
            //         point = point.Field(keyValue.Key, valBool);
            //     else if (keyValue.Value is string valString)
            //         point = point.Field(keyValue.Key, valString);
            // }
            // WriteApi.WritePoint(point);
            // return Task.CompletedTask;

            var info = new DynamicInfluxRow();
            if (parsedData.DateTime.HasValue)
                info.Timestamp = parsedData.DateTime.Value;
            if (!string.IsNullOrEmpty(parsedData.DevEUI))
                info.Tags.Add("DevEUI", parsedData.DevEUI);
            foreach (var keyValue in keyValues) 
            {
                info.Fields.Add(keyValue.Key, keyValue.Value);
            }

            _logger.LogInformation($"Tags: {string.Join(", ", info.Tags.Select(s => $"{s.Key}={s.Value}"))}");
            _logger.LogInformation($"Fields: {string.Join(", ", info.Fields.Select(s => $"{s.Key}={s.Value}"))}");

            await InfluxClient.WriteAsync(Database, Measurement, new DynamicInfluxRow[] { info });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    InfluxClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}