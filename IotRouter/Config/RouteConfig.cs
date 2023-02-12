using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace IotRouter
{
    public class RouteConfig
    {
        public class TypeConfigReference
        {
            public string Name { get; set; }
            public IConfigurationSection Config { get; set; }
        }

        public class TypeConfigReferenceWithDisable : TypeConfigReference
        {
            public bool Disabled { get; set; }
        }

        public class DeviceMappingEntry
        {
            public string DevEUI { get; set; }
            public TypeConfigReference Processor { get; set; }
            public IEnumerable<TypeConfigReferenceWithDisable> Destinations { get; set; }
        }

        public bool Disabled { get; set; }
        public TypeConfigReference Listener { get; set; }
        public TypeConfigReference Parser { get; set; }
        public IEnumerable<DeviceMappingEntry> DeviceMapping { get; set; }
    }
}
