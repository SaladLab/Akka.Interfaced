using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestCounterActor : InterfacedActor, ICounter
    {
        private List<int> _values = new List<int>();

        Task ICounter.IncCounter(int delta)
        {
            _values.Add(delta);
            return Task.FromResult(0);
        }

        Task<int> ICounter.GetCounter()
        {
            return Task.FromResult(_values.Sum());
        }
    }
}
