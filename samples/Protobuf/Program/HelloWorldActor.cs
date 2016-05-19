using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Protobuf.Interface;

namespace Protobuf.Program
{
    public class HelloWorldActor : InterfacedActor, IHelloWorld
    {
        private int _helloCount;

        async Task<string> IHelloWorld.SayHello(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name");
            await Task.Delay(1);
            _helloCount += 1;
            return string.Format("Hello {0}!", name);
        }

        Task<int> IHelloWorld.GetHelloCount()
        {
            return Task.FromResult(_helloCount);
        }
    }
}
