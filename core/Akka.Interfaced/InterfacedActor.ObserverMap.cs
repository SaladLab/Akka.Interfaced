using System.Collections.Generic;

namespace Akka.Interfaced
{
    internal class InterfacedActorObserverMap
    {
        private int _lastIssuedObserverId;
        private readonly Dictionary<int, object> _observerContextMap = new Dictionary<int, object>();

        public int IssueId()
        {
            return ++_lastIssuedObserverId;
        }

        public void AddContext(int observerId, object context)
        {
            _observerContextMap.Add(observerId, context);
        }

        public object GetContext(int observerId)
        {
            object context;
            return _observerContextMap.TryGetValue(observerId, out context) ? context : null;
        }

        public bool RemoveContext(int observerId)
        {
            return _observerContextMap.Remove(observerId);
        }
    }
}
