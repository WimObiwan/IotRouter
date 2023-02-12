using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IotRouter
{
    public interface IDestination
    {
        string Name { get; }

        Task SendAsync(ParsedData parsedData);
    }
}