using System.Threading.Tasks;
using Akka.Interfaced;

namespace HelloWorld.Interface
{
    public interface IGreeter : IInterfacedActor
    {
        Task<string> Greet(string name);
        Task<int> GetCount();
    }
}
