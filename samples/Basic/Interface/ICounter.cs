using System.Threading.Tasks;
using Akka.Interfaced;

namespace Basic.Interface
{
    public interface ICounter : IInterfacedActor
    {
        Task IncCounter(int delta);
        Task<int> GetCounter();
    }
}
