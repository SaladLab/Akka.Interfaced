using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestStartStopEvent : InterfacedActor<TestStartStopEvent>, IWorker
    {
        protected override async Task OnPreStart()
        {
            Console.WriteLine("OnPreStart() Enter");
            await Task.Delay(10);
            Console.WriteLine("OnPreStart() Mid");
            await Task.Delay(10);
            Console.WriteLine("OnPreStart() Leave");
        }

        protected override async Task OnPreStop()
        {
            Console.WriteLine("OnPreStop() Enter");
            await Task.Delay(10);
            Console.WriteLine("OnPreStop() Mid");
            await Task.Delay(10);
            Console.WriteLine("OnPreStop() Leave");
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
