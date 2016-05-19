using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    public class MessageDispatcher
    {
        private readonly Dictionary<Type, MessageHandlerItem> _handlerTable;

        public MessageDispatcher(Dictionary<Type, MessageHandlerItem> handlerTable)
        {
            _handlerTable = handlerTable;
        }

        public MessageHandlerItem GetHandler(Type type)
        {
            MessageHandlerItem item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }
    }
}
