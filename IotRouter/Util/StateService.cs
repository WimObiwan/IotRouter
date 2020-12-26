using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IotRouter
{
    public class StateService : IStateService
    {
        string stateDirectory;
        ILogger<StateService> _logger;

        public StateService(IOptions<Config> config, ILogger<StateService> logger)
        {
            string baseDirectory = config.Value.BaseDirectory;
            if (string.IsNullOrEmpty(baseDirectory))
                baseDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iotrouter");
            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(baseDirectory);
            stateDirectory = Path.Join(baseDirectory, "state");
            if (!Directory.Exists(stateDirectory))
                Directory.CreateDirectory(stateDirectory);
            _logger = logger;
        }

        public async Task<T> LoadStateAsync<T>(string context, Func<T> creator)
        {
            string path = Path.Join(stateDirectory, context);
            try
            {
                if (File.Exists(path))
                {
                    using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream);
                    }
                }
            }
            catch (Exception x)
            {
                _logger.LogWarning(x, $"Could not load state for {context}");
            }
            return creator();
        }

        public async Task StoreStateAsync<T>(string context, T state)
        {
            try
            {
                string path = Path.Join(stateDirectory, context);
                using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    await System.Text.Json.JsonSerializer.SerializeAsync<T>(stream, state);
                }
            }
            catch (Exception x)
            {
                _logger.LogWarning(x, $"Could not store state for {context}");
            }
        }
    }
}
