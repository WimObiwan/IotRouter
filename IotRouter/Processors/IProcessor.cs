using System.Collections.Generic;
using System.Threading.Tasks;

namespace IotRouter
{
    public interface IProcessor
    {
        string Name { get; }
        
        Task<bool> Process(ParsedData parsedData);
    }
}