using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoGeneric
    {
        private ActorSystem _system;

        public DemoGeneric(ActorSystem system, string[] args)
        {
            _system = system;
        }

        private class GreetingActor : InterfacedActor, IGreeter<string>
        {
            private int _count;

            Task<string> IGreeter<string>.Greet(string name)
            {
                _count += 1;
                return Task.FromResult($"Hello {name}!");
            }

            Task<string> IGreeter<string>.Greet<U>(U name)
            {
                _count += 1;
                return Task.FromResult($"Hello {name}!");
            }

            Task<int> IGreeter<string>.GetCount()
            {
                return Task.FromResult(_count);
            }
        }

        private async Task DemoCommunicateWithGenericActor()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = actor.Cast<GreeterRef<string>>();
            Console.WriteLine(await greeter.Greet("World"));
            Console.WriteLine(await greeter.Greet(2016));
            Console.WriteLine(await greeter.GetCount());
            actor.Tell(InterfacedPoisonPill.Instance);
        }
    }
}
