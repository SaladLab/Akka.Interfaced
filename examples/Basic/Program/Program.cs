using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Event;
using Basic.Interface;

namespace Basic.Program
{
    class Program
    {
        private static ActorSystem System;

        static void Main(string[] args)
        {
            System = ActorSystem.Create("MySystem");

            DeadRequestProcessingActor.Install(System);

            if (true)
            {
                var actor = System.ActorOf<TestActor>();
                TestCalculator(actor).Wait();
                TestCounter(actor).Wait();
                TestWorker(actor).Wait();
            }

            if (true)
            {
                var actor = System.ActorOf<TestActor>();
                var actorAnother = System.ActorOf<TestCounterActor>();
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
                TestEvent().Wait();
            }

            if (true)
            {
                TestStartStopEvent().Wait();
            }

            Console.WriteLine("Enter to quit");
            Console.ReadLine();
        }

        static async Task TestCalculator(IActorRef actor)
        {
            var c = new CalculatorRef(actor);
            Console.WriteLine(await c.Concat("Hello", "World"));
            Console.WriteLine(await c.Sum(1, 2));
        }

        static async Task TestCounter(IActorRef actor)
        {
            var c = new CounterRef(actor);
            await c.WithRequestWaiter(null).IncCounter(1);
            await c.WithRequestWaiter(null).IncCounter(2);
            await c.WithRequestWaiter(null).IncCounter(3);
            Console.WriteLine(await c.GetCounter());
        }

        static async Task TestWorker(IActorRef actor)
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

        static async Task TestProxyCounter()
        {
            var counter = System.ActorOf<TestCounterActor>();
            var proxy = System.ActorOf(Props.Create<TestProxyCounterActor>(counter));

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

        static async Task TestOverloaded()
        {
            var overloaded = System.ActorOf<TestOverloadedActor>();

            var o = new OverloadedRef(overloaded);
            Console.WriteLine("Min = {0}", await o.Min(2, 1));
            Console.WriteLine("Min = {0}", await o.Min(5, 4, 3));
            Console.WriteLine("Min = {0}", await o.Min(9, 8, 7, 6));
        }

        static async Task TestException()
        {
            var actor = System.ActorOf<TestExceptionActor>();
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
                    notificationMessage.Message.Invoke(Observer);
            }
        }

        static async Task TestEvent()
        {
            var actor = System.ActorOf<TestEventGenerator>();
            var g = new EventGeneratorRef(actor);

            var channel = new RawNotificationChannel<IEventObserver> {Observer = new RawEventObserver()};
            var observer = new EventObserver(channel, 1);
            await g.Subscribe(observer);
            await g.Buy("Apple", 100);
            await g.Sell("Banana", 50);
            await g.Unsubscribe(observer);

            // var x = CreateObserver<TestEventGenerator>();
        }

        static async Task TestStartStopEvent()
        {
            var actor = System.ActorOf<TestStartStopEvent>();
            var w = new WorkerRef(actor);
            Console.WriteLine(w.Actor.Path);
            w.Atomic("1");
            w.Atomic("2");
            // w.Reentrant("2");
            await w.Actor.GracefulStop(TimeSpan.FromSeconds(10), InterfacedPoisonPill.Instance);
        }
    }
}
