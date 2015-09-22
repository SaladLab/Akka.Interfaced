using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IAsyncInvokable
    {
        Task<IValueGetable> Invoke(object target);
    }
}
