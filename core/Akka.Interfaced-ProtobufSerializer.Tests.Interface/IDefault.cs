using System.Threading.Tasks;
using Akka.Interfaced;

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    public interface IDefault : IInterfacedActor
    {
        Task Call(int a, int b, string c);
        Task CallWithDefault(int a = 1, int b = 2, string c = "Test");
    }
}
