using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IWorker : IInterfacedActor
    {
        Task Atomic(int id);
        Task Reentrant(int id);
    }
}
