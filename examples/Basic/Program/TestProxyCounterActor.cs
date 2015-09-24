using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestProxyCounterActor : InterfacedActor<TestProxyCounterActor>, ICounter
    {
        private CounterRef _counter;

        public TestProxyCounterActor(IActorRef calculator)
        {
            _counter = new CounterRef(calculator);
        }

        Task ICounter.IncCounter(int delta)
        {
            _counter.IncCounter(delta);
            return Task.FromResult(0);
        }

        async Task<int> ICounter.GetCounter()
        {
            var ret = await _counter.WithRequestWaiter(this).GetCounter();
            return ret;
        }
    }
}
