using System.Threading.Tasks;
using Akka.Interfaced;

namespace PingpongInterface
{
    public interface IServer : IInterfacedActor
    {
        Task<int> Echo(int value);
    }
}
