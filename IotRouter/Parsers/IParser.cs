using System.Collections.Generic;

namespace IotRouter
{
    public interface IParser
    {
        string Name { get; }
        
        ParsedData Parse(byte[] data);
    }
}