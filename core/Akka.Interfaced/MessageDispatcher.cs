using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    public class MessageDispatcher<T>
        where T : class
    {
        private readonly Dictionary<Type, MessageHandlerItem<T>> _handlerTable;

        public MessageDispatcher(Dictionary<Type, MessageHandlerItem<T>> handlerTable)
        {
            _handlerTable = handlerTable;
        }

        public MessageHandlerItem<T> GetHandler(Type type)
        {
            MessageHandlerItem<T> item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }
    }
}
