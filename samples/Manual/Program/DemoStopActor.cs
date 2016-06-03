using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoStopActor
    {
        private ActorSystem _system;

        public DemoStopActor(ActorSystem system, string[] args)
        {
            _system = system;
        }

        public class MyActor : InterfacedActor
        {
            [MessageHandler, Reentrant]
            private async Task Handle(string message)
            {
                Console.WriteLine($"Handle({message}) Begin");
                await Task.Delay(10);
                Console.WriteLine($"Handle({message}) End");
            }

            protected override async Task OnGracefulStop()
            {
                Console.WriteLine("OnGracefulStop() Begin");
                await Task.Delay(10);
                Console.WriteLine("OnGracefulStop() End");
            }

            protected override void PostStop()
            {
                Console.WriteLine("PostStop()");
            }
        }

        private async Task DemoInterfacedPoisonPill()
        {
            var actor = _system.ActorOf<MyActor>();
            actor.Tell("Test");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }

        private async Task DemoPoisonPill()
        {
            var actor = _system.ActorOf<MyActor>();
            actor.Tell("Test");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), PoisonPill.Instance);
        }
    }
}
