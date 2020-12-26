using System;
using System.Threading.Tasks;

namespace IotRouter
{
    public interface IStateService
    {
        Task<T> LoadStateAsync<T>(string context, Func<T> creator);
        Task StoreStateAsync<T>(string context, T state);
    }
}
