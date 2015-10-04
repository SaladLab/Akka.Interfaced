using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    public class RequestDispatcher<T> where T : class
    {
        private Dictionary<Type, RequestHandlerItem<T>> _handlerTable;

        public RequestDispatcher()
        {
            _handlerTable = RequestHandlerBuilder<T>.BuildTable();
        }

        public RequestHandlerItem<T> GetHandler(Type type)
        {
            RequestHandlerItem<T> item;
            return _handlerTable.TryGetValue(type, out item) ? item : null;
        }
    }
}
