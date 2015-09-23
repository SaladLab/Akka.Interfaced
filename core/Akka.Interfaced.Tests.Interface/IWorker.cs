using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface IWorker : IInterfacedActor
    {
        Task Atomic(int id);
        Task Reentrant(int id);
    }
}
