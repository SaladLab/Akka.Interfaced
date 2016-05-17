using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    public class NotificationDispatcher<T>
        where T : class
    {
        private readonly Dictionary<Type, NotificationHandlerItem<T>> _handlerTable;

        public NotificationDispatcher(Dictionary<Type, NotificationHandlerItem<T>> handlerTable)
        {
            _handlerTable = handlerTable;
        }

        public NotificationHandlerItem<T> GetHandler(Type type)
        {
            NotificationHandlerItem<T> item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }
    }
}
