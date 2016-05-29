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
    }
}
