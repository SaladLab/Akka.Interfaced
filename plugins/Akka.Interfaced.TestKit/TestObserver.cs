using System;

namespace Akka.Interfaced.TestKit
{
    public class TestObserver : IInterfacedObserver
    {
        public int Id { get; }
        public event Action<IInvokable> Notified;

        public TestObserver(int id)
        {
            Id = id;
        }

        public void Notify(IInvokable e)
        {
            Notified?.Invoke(e);
        }
    }
}
