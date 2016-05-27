using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    [ResponsiveException(typeof(ArgumentException), typeof(InvalidOperationException))]
    public class TestExceptionActor : InterfacedActor, ICounter
    {
        private int _counter;

        async Task ICounter.IncCounter(int delta)
        {
            if (delta <= 0)
                throw new ArgumentException("Delta should be positive");

            await Task.Delay(0);

            _counter += delta;
            if (_counter >= 10)
                Context.Stop(Self);
        }

        async Task<int> ICounter.GetCounter()
        {
            await Task.Delay(0);

            if (_counter == 2)
                throw new InvalidOperationException("Counter == 2");

            return _counter;
        }
    }
}
