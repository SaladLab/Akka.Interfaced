using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public enum ThrowExceptionType
    {
        None,
        ResponsiveByWrap,
        ResponsiveByFilter,
        Fault,
    }

    public interface IBasic : IInterfacedActor
    {
        Task Call();
        Task CallWithParameter(int value);
        Task<int> CallWithReturn();
        Task<int> CallWithParameterAndReturn(int value);
        Task<int> ThrowException(ThrowExceptionType type);
    }
}
