using System;
using System.Collections.Generic;

public class ParsedData
{
    public class KeyValue
    {
        public string Key { get; private set; }
        public object Value { get; private set; }

        private KeyValue(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public KeyValue(string key, decimal value) 
        : this(key, (object)value)
        {
        }
    }

    public string DevEUI { get; private set; }
    public DateTime? DateTime { get; private set; }
    public IEnumerable<KeyValue> KeyValues { get; private set; }

    public ParsedData(string devEUI, DateTime? dateTime, IEnumerable<KeyValue> keyValues)
    {
        DevEUI = devEUI;
        DateTime = dateTime;
        KeyValues = keyValues;
    }
}
