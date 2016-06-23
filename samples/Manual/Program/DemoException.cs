using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoException
    {
        private ActorSystem _system;

        public DemoException(ActorSystem system, string[] args)
        {
            _system = system;
        }

        private class GreetingActor : InterfacedActor, IGreeter
        {
            private int _count;

            protected override void PostRestart(Exception reason)
            {
                Console.WriteLine("PostRestart");
            }

            Task<string> IGreeter.Greet(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(nameof(name));

                _count += 1;
                return Task.FromResult($"Hello {name}!");
            }

            Task<int> IGreeter.GetCount()
            {
                return Task.FromResult(_count);
            }
        }

        private async Task DemoFaultException()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = actor.Cast<GreeterRef>();

            try
            {
                await greeter.Greet(null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine(await greeter.Greet("World"));
        }

        private class GreetingActorWithResponsiveException : InterfacedActor, IGreeter
        {
            private int _count;

            protected override void PostRestart(Exception reason)
            {
                Console.WriteLine("PostRestart");
            }

            [ResponsiveException(typeof(ArgumentException))]
            Task<string> IGreeter.Greet(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(nameof(name));

                _count += 1;
                return Task.FromResult($"Hello {name}!");
            }

            Task<int> IGreeter.GetCount()
            {
                return Task.FromResult(_count);
            }
        }

        private async Task DemoResponsiveException()
        {
            var actor = _system.ActorOf<GreetingActorWithResponsiveException>();
            var greeter = actor.Cast<GreeterRef>();

            try
            {
                await greeter.Greet(null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine(await greeter.Greet("World"));
        }
    }
}
