using System.Collections.Generic;

public class DeviceMapping
{
    public string DevEUI { get; set; }
    public string ProcessorName { get; set; }
    public IList<string> DestinationNames { get; set; }
}