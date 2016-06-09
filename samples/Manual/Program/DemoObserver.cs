using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoObserver
    {
        private ActorSystem _system;

        public DemoObserver(ActorSystem system, string[] args)
        {
            _system = system;
        }

        private class GreetingActor : InterfacedActor, IGreeterWithObserver
        {
            private int _count;
            private List<IGreetObserver> _observers = new List<IGreetObserver>();

            Task<string> IGreeter.Greet(string name)
            {
                // send a notification 'Event' to all observers
                _observers.ForEach(o => o.Event($"Greet({name})"));
                _count += 1;
                return Task.FromResult($"Hello {name}!");
            }

            Task<int> IGreeter.GetCount()
            {
                return Task.FromResult(_count);
            }

            Task IGreeterWithObserver.Subscribe(IGreetObserver observer)
            {
                _observers.Add(observer);
                return Task.FromResult(true);
            }

            Task IGreeterWithObserver.Unsubscribe(IGreetObserver observer)
            {
                _observers.Remove(observer);
                return Task.FromResult(true);
            }
        }

        private class GreetObserverDisplay : IGreetObserver
        {
            void IGreetObserver.Event(string message)
            {
                Console.WriteLine($"Event: {message}");
            }
        }

        private async Task DemoObjectNotificationChannel()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = new GreeterWithObserverRef(actor);
            await greeter.Subscribe(ObjectNotificationChannel.Create<IGreetObserver>(new GreetObserverDisplay()));
            await greeter.Greet("World");
            await greeter.Greet("Actor");
            actor.Tell(InterfacedPoisonPill.Instance);
        }

        private class TestActor : InterfacedActor, IGreetObserver
        {
            [MessageHandler]
            private async Task Handle(string message)
            {
                var actor = Context.ActorOf<GreetingActor>();
                var greeter = new GreeterWithObserverRef(actor);
                await greeter.Subscribe(CreateObserver<IGreetObserver>());
                await greeter.Greet("World");
                await greeter.Greet("Actor");
            }

            void IGreetObserver.Event(string message)
            {
                Console.WriteLine($"Event: {message}");
            }
        }

        private async Task DemoActorNotificationChannel()
        {
            var actor = _system.ActorOf<TestActor>();
            actor.Tell("Test");
            await Task.Delay(100); // waits for a test actor to receive a notification message.
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }
}
