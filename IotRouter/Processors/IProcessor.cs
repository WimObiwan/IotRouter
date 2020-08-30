using System.Collections.Generic;

namespace IotRouter
{
    public interface IProcessor
    {
        string Name { get; }
        
        bool Process(ParsedData parsedData);
    }
}