using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;
using Newtonsoft.Json;
using System.Threading;
using SlimHttp.Interface;

namespace SlimHttp.Program.Server
{
    public class CounterActor : InterfacedActor<CounterActor>, ICounter
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
