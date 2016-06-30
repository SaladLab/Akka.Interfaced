using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    internal delegate ResponseMessage RequestHandler(object self, RequestMessage request, Action<ResponseMessage, Exception> onCompleted);
    internal delegate Task<ResponseMessage> RequestAsyncHandler(object self, RequestMessage request, Action<ResponseMessage, Exception> onCompleted);

    internal class RequestHandlerItem
    {
        public Type InterfaceType;
        public bool IsReentrant;
        public RequestHandler Handler;
        public RequestAsyncHandler AsyncHandler;

        // for generic method, GenericHandlerBuilder will be used to construct the handler when parameter types are ready.
        public bool IsGeneric;
        public Func<Type, RequestHandlerItem> GenericHandlerBuilder;
    }
}
