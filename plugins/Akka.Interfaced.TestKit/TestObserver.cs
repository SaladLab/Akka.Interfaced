using System.Collections.Generic;

namespace Akka.Interfaced.TestKit
{
    public class TestObserver : IInterfacedObserver
    {
        public int Id { get; }
        public List<IInvokable> Events { get; }

        public TestObserver(int id)
        {
            Id = id;
            Events = new List<IInvokable>();
        }
    }
}
