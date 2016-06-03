using System;
using System.Threading.Tasks;

namespace Akka.Interfaced.SlimClient.Tests
{
    public interface IBasic : IInterfacedActor
    {
        Task Call();
        Task CallWithParameter(int value);
        Task<int> CallWithReturn();
        Task<int> CallWithParameterAndReturn(int value);
        Task<int> ThrowException(bool throwException);
        Task<IBasic> GetSelf();
    }
}
