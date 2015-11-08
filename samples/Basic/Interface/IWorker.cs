using System.Threading.Tasks;
using Akka.Interfaced;

namespace Basic.Interface
{
    public interface IWorker : IInterfacedActor
    {
        Task Atomic(string name);
        Task Reentrant(string name);
    }
}
