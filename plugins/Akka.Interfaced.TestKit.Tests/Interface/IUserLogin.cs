using System.Threading.Tasks;

namespace Akka.Interfaced.TestKit.Tests
{
    public interface IUserLogin : IInterfacedActor
    {
        Task<IUser> Login(string id, string password, IUserObserver observer);
    }
}
