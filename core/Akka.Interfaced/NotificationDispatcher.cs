using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    public class NotificationDispatcher
    {
        private readonly Dictionary<Type, NotificationHandlerItem> _handlerTable;

        public NotificationDispatcher(Dictionary<Type, NotificationHandlerItem> handlerTable)
        {
            _handlerTable = handlerTable;
        }

        public NotificationHandlerItem GetHandler(Type type)
        {
            NotificationHandlerItem item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }
    }
}
