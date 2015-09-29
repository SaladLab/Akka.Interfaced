using System;
using System.Threading.Tasks;

namespace DispatchPerformance
{
    public interface IInterfacedMessage
    {
        Type GetInterfaceType();
    }

    public interface IInvokable
    {
        IValueGetable Invoke(object target);
    }

    public interface IAsyncInvokable
    {
        Task<IValueGetable> Invoke(object target);
    }

    public interface IValueGetable
    {
        object Value { get; }
    }
}
