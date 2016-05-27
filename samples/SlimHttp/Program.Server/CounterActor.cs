using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using SlimHttp.Interface;

namespace SlimHttp.Program.Server
{
    [ResponsiveException(typeof(ArgumentOutOfRangeException))]
    public class CounterActor : InterfacedActor, ICounter
    {
        private int _counter = 0;

        Task ICounter.IncCounter(int delta)
        {
            if (delta <= 0) throw new ArgumentOutOfRangeException("delta");
            _counter += delta;
            return Task.FromResult(true);
        }

        Task<int> ICounter.GetCounter()
        {
            return Task.FromResult(_counter);
        }
    }
}
