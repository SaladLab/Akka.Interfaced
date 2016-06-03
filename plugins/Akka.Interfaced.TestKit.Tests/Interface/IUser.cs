using System.Threading.Tasks;

namespace Akka.Interfaced.TestKit.Tests
{
    public interface IUser : IInterfacedActor
    {
        Task<string> GetId();
        Task Say(string message);
    }
}
