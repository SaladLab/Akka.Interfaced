using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoMessageHandler
    {
        private ActorSystem _system;

        public DemoMessageHandler(ActorSystem system, string[] args)
        {
            _system = system;
        }

        public class MyActor : ReceiveActor
        {
            public MyActor()
            {
                Receive<string>(message =>
                {
                    Console.WriteLine($"HandleString({message})");
                });
                Receive<int>(message =>
                {
                    Console.WriteLine($"HandleInt({message})");
                });
            }
        }

        private async Task DemoReceiveActor()
        {
            var actor = _system.ActorOf<MyActor>();
            actor.Tell("Hello");
            actor.Tell(1);
            await actor.GracefulStop(TimeSpan.FromMinutes(1));
        }

        public class MyActor2 : InterfacedActor
        {
            [MessageHandler]
            private void Handle(string message)
            {
                Console.WriteLine($"HandleString({message})");
            }

            [MessageHandler]
            private void Handle(int message)
            {
                Console.WriteLine($"HandleInt({message})");
            }
        }

        private async Task DemoInterfacedActor()
        {
            var actor = _system.ActorOf<MyActor2>();
            actor.Tell("Hello");
            actor.Tell(1);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }
}
