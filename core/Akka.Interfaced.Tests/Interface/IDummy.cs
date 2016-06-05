using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface IDummy : IInterfacedActor
    {
        Task<object> Call(object param);
    }

    public interface IDummyEx : IDummy
    {
        Task<object> CallEx(object param);
    }

    public interface IDummyEx2 : IDummy
    {
        Task<object> CallEx2(object param);
    }

    public interface IDummyExFinal : IDummyEx, IDummyEx2
    {
        Task<object> CallExFinal(object param);
    }
}
