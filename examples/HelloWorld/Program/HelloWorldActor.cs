using System.Threading.Tasks;
using Akka.Interfaced;
using HelloWorld.Interface;

namespace HelloWorld.Program
{
    public class HelloWorldActor : InterfacedActor<HelloWorldActor>, IHelloWorld
    {
        private int _helloCount;

        async Task<string> IHelloWorld.SayHello(string name)
        {
            await Task.Delay(100);
            _helloCount += 1;
            return string.Format("Hello {0}!", name);
        }

        Task<int> IHelloWorld.GetHelloCount()
        {
            return Task.FromResult(_helloCount);
        }
    }
}
