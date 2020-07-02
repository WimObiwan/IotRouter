using System.Collections.Generic;

namespace IotRouter
{
    public interface IRoute
    {
        string Listener { get; set; }
        string Parser { get; set; }
        IDictionary<string, IEnumerable<string>> DeviceMapping { get; set; }
    }
}