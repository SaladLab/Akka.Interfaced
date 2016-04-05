using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Akka.Actor;
using Akka.Interfaced;
using SlimHttp.Interface;

namespace SlimHttp.Program.Server
{
    internal class Program
    {
        public static ActorSystem System { get; private set; }
        private static HttpSelfHostServer s_httpServer;

        private static void Main(string[] args)
        {
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

        private static async Task StartAsync()
        {
            var serviceUri = "http://localhost:9000";
            var httpConfig = new HttpSelfHostConfiguration(serviceUri);
            httpConfig.MapHttpAttributeRoutes();

            s_httpServer = new HttpSelfHostServer(httpConfig);
            try
            {
                await s_httpServer.OpenAsync();
            }
            catch (System.ServiceModel.AddressAccessDeniedException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("");
                Console.WriteLine("type following command in administrative commandline to runs this program");
                Console.WriteLine("> netsh http add urlacl http://+:9000/ user=Everyone");
                Console.WriteLine("");
            }
        }

        private static async Task StopAsync()
        {
            if (s_httpServer != null)
            {
                await s_httpServer.CloseAsync();
                s_httpServer = null;
            }
        }
    }
}
