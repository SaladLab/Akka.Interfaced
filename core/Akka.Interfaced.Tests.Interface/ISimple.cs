using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface ISimple : IInterfacedActor
    {
        Task Call();
        Task CallWithParameter(int value);
        Task<int> CallWithReturn();
        Task<int> CallWithParameterAndReturn(int value);
        Task<int> ThrowException(bool throwException);
    }
}
