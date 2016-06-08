using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;
using Akka.Interfaced.LogFilter;
using NLog;

namespace Manual
{
    public class DemoLogFilter
    {
        private ActorSystem _system;

        public DemoLogFilter(ActorSystem system, string[] args)
        {
            _system = system;
        }

        [Log, ResponsiveException(typeof(ArgumentException))]
        private class GreetingActor : InterfacedActor, IGreeterSync
        {
            private static Logger s_logger = LogManager.GetCurrentClassLogger();
            private int _count;

            string IGreeterSync.Greet(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException(nameof(name));
                _count += 1;
                return $"Hello {name}!";
            }

            int IGreeterSync.GetCount()
            {
                return _count;
            }

            [MessageHandler]
            private void OnMessage(string message)
            {
                Console.WriteLine("Message: " + message);
            }
        }

        private async Task DemoLogRequest()
        {
            var actor = _system.ActorOf<GreetingActor>();
            var greeter = new GreeterRef(actor);
            Console.WriteLine(await greeter.Greet("World"));
            try
            {
                Console.WriteLine(await greeter.Greet(null));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType().Name);
            }
            Console.WriteLine(await greeter.GetCount());
            greeter.Actor.Tell("Bye!");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }
}
