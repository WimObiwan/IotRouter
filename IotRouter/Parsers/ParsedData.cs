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

        public KeyValue(string key, string value) 
            : this(key, (object)value)
        {
        }

        public KeyValue(string key, int value) 
            : this(key, (object)value)
        {
        }

        public KeyValue(string key, double value) 
        : this(key, (object)value)
        {
        }

        public KeyValue(string key, decimal value) 
            : this(key, (object)value)
        {
        }
    }

    public string DevEUI { get; private set; }
    public DateTime? DateTime { get; private set; }
    public IList<KeyValue> KeyValues { get; private set; }

    public ParsedData(string devEUI, DateTime? dateTime, IList<KeyValue> keyValues)
    {
        DevEUI = devEUI;
        DateTime = dateTime;
        KeyValues = keyValues;
    }
}
