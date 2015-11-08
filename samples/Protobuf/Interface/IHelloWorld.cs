using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Protobuf.Interface
{
    public interface IHelloWorld : IInterfacedActor
    {
        Task<string> SayHello(string name);
        Task<int> GetHelloCount();
    }
}
