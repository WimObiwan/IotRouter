using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public abstract class Parser : IParser
    {
        protected ILogger _logger;

        public string Name { get; private set; }

        public Parser(ILogger logger, string name)
        {
            _logger = logger;
            Name = name;
        }

        public abstract ParsedData Parse(byte[] data);
    }
}
