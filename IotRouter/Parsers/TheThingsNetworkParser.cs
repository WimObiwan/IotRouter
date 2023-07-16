using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace IotRouter
{
    public abstract class TheThingsNetworkParser : IParser
    {
        ILogger _logger;

        public string Name { get; private set; }

        public TheThingsNetworkParser(ILogger logger, string name)
        {
            _logger = logger;
            Name = name;
        }

        public ParsedData Parse(byte[] data)
        {
            ParserData parserData = new ParserData(data);
            return Parse(parserData);
        }

        protected abstract ParsedData Parse(ParserData parserData);
    }
}
