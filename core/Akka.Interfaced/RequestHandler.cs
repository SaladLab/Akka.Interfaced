using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate ResponseMessage RequestHandler(object self, RequestMessage request, Action<ResponseMessage> onCompleted);
    public delegate Task<ResponseMessage> RequestAsyncHandler(object self, RequestMessage request, Action<ResponseMessage> onCompleted);

    public class RequestHandlerItem
    {
        public Type InterfaceType;
        public bool IsReentrant;
        public RequestHandler Handler;
        public RequestAsyncHandler AsyncHandler;
    }
}
