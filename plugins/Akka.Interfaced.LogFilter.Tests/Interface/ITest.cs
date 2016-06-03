using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Interfaced.LogFilter.Tests
{
    public interface ITest : IInterfacedActor
    {
        Task Call(string value);
        Task CallWithActor(ITest test);
        Task<string> SayHello(string name);
        Task<int> GetHelloCount();
    }
}
