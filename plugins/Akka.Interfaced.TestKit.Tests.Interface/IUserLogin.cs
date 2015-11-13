using System.Threading.Tasks;

namespace Akka.Interfaced.TestKit.Tests
{
    public interface IUserLogin : IInterfacedActor
    {
        Task<int> Login(string id, string password, int observerId);
    }
}
