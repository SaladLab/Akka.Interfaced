using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoActor
    {
        private ActorSystem _system;

        public DemoActor(ActorSystem system, string[] args)
        {
            _system = system;
        }

        private class GreetingActor : InterfacedActor, IGreeter
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

        private async Task DemoCommunicateWithActor()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = new GreeterRef(actor);
            Console.WriteLine(await greeter.Greet("World"));
            Console.WriteLine(await greeter.Greet("Actor"));
            Console.WriteLine(await greeter.GetCount());
            actor.Tell(InterfacedPoisonPill.Instance);
        }

        private class TestActor : InterfacedActor
        {
            [MessageHandler]
            private async Task Handle(string message)
            {
                var actor = Context.ActorOf<GreetingActor>();
                var greeter = new GreeterRef(actor, this);
                Console.WriteLine(await greeter.Greet("World"));
                Console.WriteLine(await greeter.Greet("Actor"));
                Console.WriteLine(await greeter.GetCount());
            }
        }

        private async Task DemoRequestWaiter()
        {
            var actor = _system.ActorOf<TestActor>();
            actor.Tell("Test");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }

        private class GreetingActorWithDelay : InterfacedActor, IGreeter
        {
            private int _count;

            async Task<string> IGreeter.Greet(string name)
            {
                _count += 1;
                await Task.Delay(100);
                return $"Hello {name}!";
            }

            Task<int> IGreeter.GetCount()
            {
                return Task.FromResult(_count);
            }
        }

        private async Task DemoTimeout()
        {
            var actor = _system.ActorOf<GreetingActorWithDelay>();
            var greeter = new GreeterRef(actor);

            await greeter.Greet("World");

            try
            {
                await greeter.WithTimeout(TimeSpan.FromMilliseconds(10)).Greet("Actor");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine(await greeter.GetCount());
            actor.Tell(InterfacedPoisonPill.Instance);
        }

        private async Task DemoFireAndForget()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = new GreeterRef(actor);
            greeter.WithNoReply().Greet("World");
            greeter.WithNoReply().Greet("Actor");
            Console.WriteLine(await greeter.GetCount());
            actor.Tell(InterfacedPoisonPill.Instance);
        }
    }
}
