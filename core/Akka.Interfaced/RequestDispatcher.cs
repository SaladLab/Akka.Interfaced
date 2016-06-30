using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    internal class RequestDispatcher
    {
        private Dictionary<Type, RequestHandlerItem> _handlerTable;

        public RequestDispatcher(Dictionary<Type, RequestHandlerItem> handlerTable)
        {
            _handlerTable = handlerTable;
        }

        public RequestHandlerItem GetHandler(Type type)
        {
            RequestHandlerItem item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }

        // this method doesn't modify _handlerTable directly because this causes a race condition.
        // two threads may call this method at the same time and one of calling loses the changes it made.
        // but caller uses this method like a cache update so loosing change is acceptable.
        internal void AddHandler(Type type, RequestHandlerItem handler)
        {
            var newTable = new Dictionary<Type, RequestHandlerItem>(_handlerTable);
            newTable.Add(type, handler);
            _handlerTable = newTable;
        }
    }
}
