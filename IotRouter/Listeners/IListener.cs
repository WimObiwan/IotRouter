using System;
using System.Threading;
using System.Threading.Tasks;

namespace IotRouter
{
    public delegate Task MessageReceivedHandler(object sender, MessageReceivedEventArgs e);

    public interface IListener : IDisposable
    {
        string Name { get; }
        event MessageReceivedHandler MessageReceived;

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}