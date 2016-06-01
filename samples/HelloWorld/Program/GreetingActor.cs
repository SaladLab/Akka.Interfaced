using System.Threading.Tasks;
using Akka.Interfaced;
using HelloWorld.Interface;

namespace HelloWorld.Program
{
    public class GreetingActor : InterfacedActor, IGreeter
    {
        private int _count;

        Task<string> IGreeter.Greet(string name)
        {
            _count += 1;
            return Task.FromResult($"Hello {name}!");
        }

        Task<int> IGreeter.GetCount()
        {
            return Task.FromResult(_count);
        }
    }
}
