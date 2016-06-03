using System.Threading.Tasks;
using Akka.Interfaced;

namespace Manual
{
    public interface IGreeter : IInterfacedActor
    {
        Task<string> Greet(string name);
        Task<int> GetCount();
    }
}
