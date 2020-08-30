using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace IotRouter
{
    public class ParserData
    {
        private JsonDocument _document;
        private JsonElement _payloadFieldsElement;
        private JsonElement _rootElement;
        private JsonElement _metadataElement;
        private JsonElement _gatewayElements;

        public ParserData(byte[] data)
        {
            _document = JsonDocument.Parse(Encoding.UTF8.GetString(data));
            _rootElement = _document.RootElement;
            _payloadFieldsElement = _rootElement.GetProperty("payload_fields"); 
            _metadataElement = _rootElement.GetProperty("metadata"); 
            _gatewayElements = _metadataElement.GetProperty("gateways");
        }

        private static ParserValue Get(JsonElement element, string key)
        {
            try
            {
                return new ParserValue(element.GetProperty(key));
            }
            catch (System.Exception x)
            {
                throw new Exception($"Element key {key} not found", x);
            }
        }

        private static bool TryGet(JsonElement element, string key, out ParserValue parserValue)
        {
            if (element.TryGetProperty(key, out JsonElement value))
            {
                parserValue = new ParserValue(value);
                return true;
            }
            else
            {
                parserValue = null;
                return false;
            }
        }

        public ParserValue GetPayloadValue(string key)
        {
            return Get(_payloadFieldsElement, key);
        }

        public bool TryGetPayloadValue(string key, out ParserValue parserValue)
        {
            return TryGet(_payloadFieldsElement, key, out parserValue);
        }

        public string GetDevEUI()
        {
            return ParserValue.AsString(_rootElement.GetProperty("hardware_serial"));
        }

        public decimal GetRSSI()
        {
            return _gatewayElements.EnumerateArray().Max(g => ParserValue.AsDecimal(g.GetProperty("rssi")));
        }

        public DateTime GetTime()
        {
            return ParserValue.AsDateTime(_metadataElement.GetProperty("time"));
        }
    }
}
