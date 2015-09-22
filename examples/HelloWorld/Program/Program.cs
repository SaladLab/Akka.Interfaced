using System;
using Akka.Actor;
using Akka.Interfaced;
using HelloWorld.Interface;

namespace HelloWorld.Program
{
    class Program
    {
        static void Main(string[] args)
        {
            var system = ActorSystem.Create("MySystem");
            DeadRequestProcessingActor.Install(system);

            // Create HelloWorldActor and get it's ref object
            var actor = system.ActorOf<HelloWorldActor>();
            var helloWorld = new HelloWorldRef(actor);

            // Make some noise
            Console.WriteLine(helloWorld.SayHello("World").Result);
            Console.WriteLine(helloWorld.SayHello("Dlrow").Result);
            Console.WriteLine(helloWorld.GetHelloCount().Result);
        }
    }
}
