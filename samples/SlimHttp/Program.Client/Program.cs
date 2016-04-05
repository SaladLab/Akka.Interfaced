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

            // Calculator

            Console.WriteLine("\n*** Calculator ***");

            var calculatorActor = new SlimActorRef() { Id = "calculator" };
            var calculator = new CalculatorRef(calculatorActor, requestWaiter, null);

            PrintResult(calculator.Concat("Hello", "World"));
            PrintResult(calculator.Concat(null, "Error"));
            PrintResult(calculator.Sum(1, 2));

            // Counter

            Console.WriteLine("\n*** Counter ***");

            var counterActor = new SlimActorRef() { Id = "counter" };
            var counter = new CounterRef(counterActor, requestWaiter, null);

            counter.IncCounter(1);
            counter.IncCounter(2);
            counter.IncCounter(3);
            PrintResult(counter.GetCounter());

            // Pedantic

            Console.WriteLine("\n*** Pedantic ***");

            var pedanticActor = new SlimActorRef() { Id = "pedantic" };
            var pedantic = new PedanticRef(pedanticActor, requestWaiter, null);

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
