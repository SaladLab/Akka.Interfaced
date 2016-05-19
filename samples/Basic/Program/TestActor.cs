using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestActor : InterfacedActor, ICalculator, ICounter, IWorker
    {
        private int _counter;

        [Log]
        Task<string> ICalculator.Concat(string a, string b)
        {
            return Task.FromResult(a + b);
        }

        [Log]
        Task<int> ICalculator.Sum(int a, int b)
        {
            return Task.FromResult(a + b);
        }

        [Log]
        Task ICounter.IncCounter(int delta)
        {
            _counter += delta;
            return Task.FromResult(0);
        }

        [Log]
        Task<int> ICounter.GetCounter()
        {
            return Task.FromResult(_counter);
        }

        async Task IWorker.Atomic(string name)
        {
            Console.WriteLine("Atomic({0}) Enter", name);
            await Task.Delay(10);
            Console.WriteLine("Atomic({0}) Mid", name);
            await Task.Delay(10);
            Console.WriteLine("Atomic({0}) Leave", name);
        }

        [Reentrant]
        async Task IWorker.Reentrant(string name)
        {
            Console.WriteLine("Reentrant({0}) Enter", name);
            await Task.Delay(10);
            Console.WriteLine("Reentrant({0}) Mid", name);
            await Task.Delay(10);
            Console.WriteLine("Reentrant({0}) Leave", name);
        }
    }
}
