using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using HelloWorld.Interface;

namespace HelloWorld.Program
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var system = ActorSystem.Create("MySystem");
            DeadRequestProcessingActor.Install(system);
            TestAsync(system).Wait();
        }

        private static async Task TestAsync(ActorSystem system)
        {
            // Create GreetingActor and make a reference pointing to an actor.
            var actor = system.ActorOf<GreetingActor>();
            var helloWorld = new GreeterRef(actor);

            // Make some noise
            Console.WriteLine(await helloWorld.Greet("World"));
            Console.WriteLine(await helloWorld.Greet("Actor"));
            Console.WriteLine(await helloWorld.GetCount());
        }
    }
}
