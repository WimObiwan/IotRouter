using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public class Route : IRoute
    {
        ILogger<Route> _logger;

        public string Listener { get; set; }
        public string Parser { get; set; }
        public IDictionary<string, DeviceMapping> DeviceMappings { get; set; }

        public Route(ILogger<Route> logger)
        {
            _logger = logger;
        }
    }
}
