using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace IotRouter
{
    public class TypeConfig
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public IConfigurationSection Config { get; set; }
    }
}
