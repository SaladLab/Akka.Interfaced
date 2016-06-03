using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoStartEvent
    {
        private ActorSystem _system;

        public DemoStartEvent(ActorSystem system, string[] args)
        {
            _system = system;
        }

        public class MyActor : InterfacedActor
        {
            protected override void PreStart()
            {
                Console.WriteLine("PreStart()");
            }

            protected override async Task OnStart(bool restarted)
            {
                Console.WriteLine($"OnStart() Begin");
                await Task.Delay(10);
                Console.WriteLine($"OnStart() End");
            }

            [MessageHandler]
            protected async Task Handle(string message)
            {
                Console.WriteLine($"Handle({message}) Begin");
                await Task.Delay(10);
                Console.WriteLine($"Handle({message}) End");
            }
        }

        private async Task DemoOnStart()
        {
            var actor = _system.ActorOf<MyActor>();
            actor.Tell("Test");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }

        public class MyActor2 : InterfacedActor
        {
            protected override void PreStart()
            {
                Console.WriteLine("PreStart()");
            }

            protected override void PostRestart(Exception cause)
            {
                Console.WriteLine($"PostRestart({cause.GetType().Name})");
            }

            protected override async Task OnStart(bool restarted)
            {
                Console.WriteLine($"OnStart({restarted}) Begin");
                await Task.Delay(10);
                Console.WriteLine($"OnStart({restarted}) End");
            }

            [MessageHandler]
            protected async Task Handle(string message)
            {
                Console.WriteLine($"Handle({message}) Throw");
                throw new InvalidOperationException(message);
            }
        }

        private async Task DemoRestart()
        {
            var actor = _system.ActorOf<MyActor2>();
            actor.Tell("Test");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }
}
