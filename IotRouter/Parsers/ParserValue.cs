using System;
using System.Globalization;
using System.Text.Json;

namespace IotRouter
{
    public class ParserValue
    {
        private JsonElement _element;

        public ParserValue(JsonElement element)
        {
            _element = element;
        }
        
        public bool IsNull()
        {
            return IsNull(_element);
        }

        public string AsString()
        {
            return AsString(_element);
        }

        public decimal AsInt()
        {
            return AsInt(_element);
        }

        public decimal AsDecimal()
        {
            return AsDecimal(_element);
        }

        public static bool IsNull(JsonElement element)
        {
            return (element.ValueKind == JsonValueKind.Null);
        }

        public static string AsString(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                default:
                    throw new NotSupportedException();
            }
        }

        public static int AsInt(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return element.GetInt32();
                case JsonValueKind.String:
                    return int.Parse(element.GetString(), CultureInfo.InvariantCulture);
                default:
                    throw new NotSupportedException();
            }
        }

        public static decimal AsDecimal(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    return element.GetDecimal();
                case JsonValueKind.String:
                    return decimal.Parse(element.GetString(), CultureInfo.InvariantCulture);
                default:
                    throw new NotSupportedException();
            }
        }

        public static DateTime AsDateTime(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetDateTime();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}