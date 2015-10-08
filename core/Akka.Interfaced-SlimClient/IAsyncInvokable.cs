using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IAsyncInvokable
    {
        Task<IValueGetable> InvokeAsync(object target);
    }
}
