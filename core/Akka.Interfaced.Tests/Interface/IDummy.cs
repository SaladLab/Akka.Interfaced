using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface IDummy : IInterfacedActor
    {
        Task<object> Call(object param);
    }
}
