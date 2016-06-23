using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoReentrantHandler
    {
        private ActorSystem _system;

        public DemoReentrantHandler(ActorSystem system, string[] args)
        {
            _system = system;
        }

        public class GreetingActor : InterfacedActor, IGreeter
        {
            private int _count;

            async Task<string> IGreeter.Greet(string name)
            {
                _count += 1;
                Console.WriteLine($"Greet({name}) Begin");
                await Task.Delay(10);
                Console.WriteLine($"Greet({name}) End");
                return $"Hello {name}!";
            }

            Task<int> IGreeter.GetCount()
            {
                Console.WriteLine("GetCount()");
                return Task.FromResult(_count);
            }
        }

        private async Task DemoAtomicMethod()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = actor.Cast<GreeterRef>();
            await Task.WhenAll(
                greeter.Greet("A"),
                greeter.Greet("B"),
                greeter.GetCount());
        }

        public class GreetingReentrantActor : InterfacedActor, IGreeter
        {
            private int _count;

            [Reentrant]
            async Task<string> IGreeter.Greet(string name)
            {
                _count += 1;
                Console.WriteLine($"Greet({name}) Begin");
                await Task.Delay(10);
                Console.WriteLine($"Greet({name}) End");
                return $"Hello {name}!";
            }

            Task<int> IGreeter.GetCount()
            {
                Console.WriteLine("GetCount()");
                return Task.FromResult(_count);
            }
        }

        private async Task DemoReentrantMethod()
        {
            var actor = _system.ActorOf<GreetingReentrantActor>();
            var greeter = actor.Cast<GreeterRef>();
            await Task.WhenAll(
                greeter.Greet("A"),
                greeter.Greet("B"),
                greeter.GetCount());
        }

        public class ServingActor : InterfacedActor, IGreeter
        {
            private bool _stopped;
            private int _count;

            protected override void PreStart()
            {
                Self.Tell("start");
            }

            [MessageHandler, Reentrant]
            private async Task Handle(string message)
            {
                if (message == "start")
                {
                    while (_stopped == false)
                    {
                        _count += 1;
                        await Task.Delay(100);
                    }
                }
                else if (message == "stop")
                {
                    _stopped = true;
                }
            }

            Task<string> IGreeter.Greet(string name)
            {
                throw new NotImplementedException();
            }

            Task<int> IGreeter.GetCount()
            {
                return Task.FromResult(_count);
            }
        }

        private async Task DemoBackgroundServing()
        {
            var actor = _system.ActorOf<ServingActor>();
            var greeter = actor.Cast<GreeterRef>();
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(200);
                Console.WriteLine(await greeter.GetCount());
                if (i == 3)
                {
                    actor.Tell("stop");
                    Console.WriteLine("stop");
                }
            }
        }
    }
}
