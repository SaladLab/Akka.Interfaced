using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;
using SlimHttp.Interface;

namespace SlimHttp.Program.Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var requestWaiter = new SlimRequestWaiter { Root = new Uri("http://localhost:9000") };

            // Greeter

            Console.WriteLine("\n*** Greeter ***");

            var greeter = new GreeterRef(new SlimActorRef("greeter"), requestWaiter, null);
            PrintResult(greeter.Greet("World"));
            PrintResult(greeter.Greet("Actor"));
            PrintResult(greeter.GetCount());

            // Calculator

            Console.WriteLine("\n*** Calculator ***");

            var calculator = new CalculatorRef(new SlimActorRef("calculator"), requestWaiter, null);
            PrintResult(calculator.Concat("Hello", "World"));
            PrintResult(calculator.Concat(null, "Error"));
            PrintResult(calculator.Sum(1, 2));

            // Counter

            Console.WriteLine("\n*** Counter ***");

            var counter = new CounterRef(new SlimActorRef("counter"), requestWaiter, null);
            counter.IncCounter(1);
            counter.IncCounter(2);
            counter.IncCounter(3);
            PrintResult(counter.GetCounter());

            // Pedantic

            Console.WriteLine("\n*** Pedantic ***");

            var pedantic = new PedanticRef(new SlimActorRef("pedantic"), requestWaiter, null);
            pedantic.TestCall().Wait();
            PrintResult(pedantic.TestOptional(null));
            PrintResult(pedantic.TestOptional(1));
            PrintResult(pedantic.TestTuple(Tuple.Create(1, "One")));
            PrintResult(pedantic.TestParams(1, 2, 3));
            PrintResult(pedantic.TestPassClass(new TestParam { Name = "Mouse", Price = 10 }));
            PrintResult(pedantic.TestReturnClass(1, 2));
        }

        private static void PrintResult<TResult>(Task<TResult> task)
        {
            try
            {
                Console.WriteLine("Result: " + task.Result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
            }
        }
    }
}
