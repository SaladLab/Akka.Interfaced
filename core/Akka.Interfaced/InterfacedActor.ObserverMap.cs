using System.Collections.Generic;

namespace Akka.Interfaced
{
    internal class InterfacedActorObserverMap
    {
        private int _lastIssuedObserverId;
        private Dictionary<int, IInterfacedObserver> _observerMap = new Dictionary<int, IInterfacedObserver>();

        public int IssueId()
        {
            return ++_lastIssuedObserverId;
        }

        public void Add(int observerId, IInterfacedObserver observer)
        {
            _observerMap.Add(observerId, observer);
        }

        public IInterfacedObserver Get(int observerId)
        {
            IInterfacedObserver observer;
            return _observerMap.TryGetValue(observerId, out observer) ? observer : null;
        }

        public bool Remove(int observerId)
        {
            return _observerMap.Remove(observerId);
        }

        public void Notify(NotificationMessage notification)
        {
            IInterfacedObserver observer;
            if (_observerMap.TryGetValue(notification.ObserverId, out observer) == false)
                return;

            notification.InvokePayload.Invoke(observer);
        }
    }
}
