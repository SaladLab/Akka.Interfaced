using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    internal class Program
    {
        private static ActorSystem s_system;

        private static void Main(string[] args)
        {
            s_system = ActorSystem.Create("MySystem");

            DeadRequestProcessingActor.Install(s_system);

            if (true)
            {
                var actor = s_system.ActorOf<TestActor>();
                TestCalculator(actor).Wait();
                TestCounter(actor).Wait();
                TestWorker(actor).Wait();
            }

            if (true)
            {
                var actor = s_system.ActorOf<TestActor>();
                var actorAnother = s_system.ActorOf<TestCounterActor>();
                TestCounter(actor).Wait();
                TestCounter(actorAnother).Wait();
            }

            if (true)
            {
                TestProxyCounter().Wait();
            }

            if (true)
            {
                TestOverloaded().Wait();
            }

            if (true)
            {
                TestException().Wait();
            }

            if (true)
            {
                TestExtendedInterface().Wait();
            }

            if (true)
            {
                TestEvent().Wait();
            }

            if (true)
            {
                TestStartStopEvent().Wait();
            }

            Console.WriteLine("Enter to quit");
            Console.ReadLine();
        }

        private static async Task TestCalculator(IActorRef actor)
        {
            var c = new CalculatorRef(actor);
            Console.WriteLine(await c.Concat("Hello", "World"));
            Console.WriteLine(await c.Sum(1, 2));
        }

        private static async Task TestCounter(IActorRef actor)
        {
            var c = new CounterRef(actor);
            c.WithNoReply().IncCounter(1);
            c.WithNoReply().IncCounter(2);
            c.WithNoReply().IncCounter(3);
            Console.WriteLine(await c.GetCounter());
        }

        private static async Task TestWorker(IActorRef actor)
        {
            var w = new WorkerRef(actor);

            Console.WriteLine("Test-1");
            await Task.WhenAll(
                w.Atomic("A"),
                w.Atomic("B"));

            Console.WriteLine("Test-2");
            await Task.WhenAll(
                w.Reentrant("A"),
                w.Reentrant("B"));

            Console.WriteLine("Test-3");
            await Task.WhenAll(
                w.Reentrant("A"),
                w.Atomic("B"),
                w.Reentrant("C"));
        }

        private static async Task TestProxyCounter()
        {
            var counter = s_system.ActorOf<TestCounterActor>();
            var proxy = s_system.ActorOf(Props.Create<TestProxyCounterActor>(counter));

            try
            {
                var c = new CounterRef(proxy);
                await c.IncCounter(1);
                await c.IncCounter(2);
                await c.IncCounter(3);
                Console.WriteLine(await c.GetCounter());
            }
            catch (Exception e)
            {
                Console.WriteLine("! " + e);
            }
        }

        private static async Task TestOverloaded()
        {
            var overloaded = s_system.ActorOf<TestOverloadedActor>();

            var o = new OverloadedRef(overloaded);
            Console.WriteLine("Min = {0}", await o.Min(2, 1));
            Console.WriteLine("Min = {0}", await o.Min(5, 4, 3));
            Console.WriteLine("Min = {0}", await o.Min(9, 8, 7, 6));
        }

        private static async Task TestException()
        {
            var actor = s_system.ActorOf<TestExceptionActor>();
            var c = new CounterRef(actor);

            try
            {
                await c.IncCounter(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("! " + e);
            }

            await c.IncCounter(1);
            await c.IncCounter(1);

            try
            {
                Console.WriteLine(await c.GetCounter());
            }
            catch (Exception e)
            {
                Console.WriteLine("! " + e);
            }

            try
            {
                await c.IncCounter(10);
                await c.IncCounter(10);
            }
            catch (Exception e)
            {
                Console.WriteLine("! " + e);
            }
        }

        private static async Task TestExtendedInterface()
        {
            Console.WriteLine("***** TestExtendedInterface *****");

            var actor = s_system.ActorOf<TestExtendedActor>();
            var c = new CounterRef(actor);
            c.WithNoReply().IncCounter(1);
            c.WithNoReply().IncCounter(2);
            c.WithNoReply().IncCounter(3);
            Console.WriteLine(await c.GetCounter());
        }

        public class RawEventObserver : IEventObserver
        {
            void IEventObserver.OnBuy(string name, int price)
            {
                Console.WriteLine("RawEventObserver.OnBuy({0}, {1})", name, price);
            }

            void IEventObserver.OnSell(string name, int price)
            {
                Console.WriteLine("RawEventObserver.OnSell({0}, {1})", name, price);
            }
        }

        public class RawNotificationChannel<T> : INotificationChannel
        {
            public T Observer { get; set; }

            void INotificationChannel.Notify(NotificationMessage notificationMessage)
            {
                if (Observer != null)
                    notificationMessage.InvokePayload.Invoke(Observer);
            }
        }

        private static async Task TestEvent()
        {
            var actor = s_system.ActorOf<TestEventGenerator>();
            var g = new EventGeneratorRef(actor);

            var channel = new RawNotificationChannel<IEventObserver> { Observer = new RawEventObserver() };
            var observer = new EventObserver(channel, 1);
            await g.Subscribe(observer);
            await g.Buy("Apple", 100);
            await g.Sell("Banana", 50);
            await g.Unsubscribe(observer);

            // var x = CreateObserver<TestEventGenerator>();
        }

        private static async Task TestStartStopEvent()
        {
            var actor = s_system.ActorOf<TestStartStopEvent>();
            var w = new WorkerRef(actor);
            Console.WriteLine(w.Actor.Path);
            w.WithNoReply().Atomic("1");
            w.WithNoReply().Atomic("2");
            // w.Reentrant("2");
            await w.Actor.GracefulStop(TimeSpan.FromSeconds(10), InterfacedPoisonPill.Instance);
        }
    }
}
