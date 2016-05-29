using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    internal class RequestDispatcher
    {
        private readonly Dictionary<Type, RequestHandlerItem> _handlerTable;

        public RequestDispatcher(Dictionary<Type, RequestHandlerItem> handlerTable)
        {
            _handlerTable = handlerTable;
        }

        public RequestHandlerItem GetHandler(Type type)
        {
            RequestHandlerItem item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }
    }
}
