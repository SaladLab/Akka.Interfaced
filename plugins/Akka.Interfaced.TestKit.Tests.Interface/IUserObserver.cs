using System.Threading.Tasks;

namespace Akka.Interfaced.TestKit.Tests
{
    public interface IUserObserver : IInterfacedObserver
    {
        void Say(string message);
    }
}
