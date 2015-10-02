using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Interfaced;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using SlimUnity.Interface;

namespace SlimUnity.Program.Server
{
    class Program
    {
        public static ActorSystem System { get; private set; }
        private static ActorService Service;

        static void Main(string[] args)
        {
            if (typeof(ICalculator) == null)
                throw new InvalidProgramException();

            System = ActorSystem.Create("MySystem");

            DeadRequestProcessingActor.Install(System);

            var counter = System.ActorOf<CounterActor>("counter");
            var calculator = System.ActorOf<CalculatorActor>("calculator");
            var pedantic = System.ActorOf<PedanticActor>("pedantic");

            StartAsync().Wait();

            Console.WriteLine("Enter to quit");
            Console.ReadLine();

            StopAsync().Wait();
        }

        static Task StartAsync()
        {
            Service = new ActorService(System);
            Service.Start(new IPEndPoint(0, 9000));
            return Task.FromResult(true);
        }

        static Task StopAsync()
        {
            if (Service != null)
            {
                Service.Stop();
                Service = null;
            }
            return Task.FromResult(true);
        }
    }
}
