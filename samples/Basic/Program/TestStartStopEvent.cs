using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestStartStopEvent : InterfacedActor, IWorker
    {
        protected override async Task OnStart(bool restarted)
        {
            Console.WriteLine("OnStart() Enter");
            await Task.Delay(10);
            Console.WriteLine("OnStart() Mid");
            await Task.Delay(10);
            Console.WriteLine("OnStart() Leave");
        }

        protected override async Task OnGracefulStop()
        {
            Console.WriteLine("OnGracefulStop() Enter");
            await Task.Delay(10);
            Console.WriteLine("OnGracefulStop() Mid");
            await Task.Delay(10);
            Console.WriteLine("OnGracefulStop() Leave");
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
