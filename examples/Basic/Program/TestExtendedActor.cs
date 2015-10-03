using System.Collections.Generic;
using System.Linq;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestExtendedActor : InterfacedActor<TestExtendedActor>, IExtendedInterface<ICounter>
    {
        private List<int> _values = new List<int>();

        [ExtendedHandler]
        void IncCounter(int delta)
        {
            _values.Add(delta);
        }

        [ExtendedHandler]
        int GetCounter()
        {
            return _values.Sum();
        }
    }
}
