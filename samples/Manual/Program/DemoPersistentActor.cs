using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.Persistence;
using Akka.Persistence;

namespace Manual
{
    public class DemoPersistentActor
    {
        private ActorSystem _system;

        public DemoPersistentActor(ActorSystem system, string[] args)
        {
            _system = system;
        }

        public class GreetEvent
        {
            public string Name;
        }

        public class GreeterState
        {
            public int GreetCount;
            public int TotalNameLength;

            public void OnGreet(GreetEvent e)
            {
                GreetCount += 1;
                TotalNameLength += e.Name.Length;
            }
        }

        public class PersistentGreetingActor : InterfacedPersistentActor, IGreeter
        {
            private GreeterState _state = new GreeterState();

            public override string PersistenceId { get; }

            public PersistentGreetingActor(string id)
            {
                PersistenceId = id;
            }

            [MessageHandler]
            private void OnRecover(SnapshotOffer snapshot)
            {
                _state = (GreeterState)snapshot.Snapshot;
            }

            [MessageHandler]
            private void OnRecover(GreetEvent e)
            {
                _state.OnGreet(e);
            }

            async Task<string> IGreeter.Greet(string name)
            {
                var e = new GreetEvent { Name = name };
                await PersistTaskAsync(e);
                _state.OnGreet(e);
                return $"Hello {name}!";
            }

            Task<int> IGreeter.GetCount()
            {
                return Task.FromResult(_state.GreetCount);
            }
        }

        private async Task DemoRun()
        {
            // create actor, change state of it, and destroy it.
            var a = _system.ActorOf(Props.Create(() => new PersistentGreetingActor("greeter1")));
            var g = a.Cast<GreeterRef>();
            await g.Greet("World");
            await g.Greet("Actor");
            Console.WriteLine("1st: " + await g.GetCount());
            await a.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // create actor, and check saved state.
            var a2 = _system.ActorOf(Props.Create(() => new PersistentGreetingActor("greeter1")));
            var g2 = a2.Cast<GreeterRef>();
            Console.WriteLine("2nd: " + await g2.GetCount());
            await g2.Greet("More");
            Console.WriteLine("3rd: " + await g2.GetCount());
            await a2.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }
}
