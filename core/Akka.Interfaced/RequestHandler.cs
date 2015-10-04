using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate ResponseMessage RequestHandler<in T>(T self, RequestMessage request, Action<ResponseMessage> onCompleted);
    public delegate Task<ResponseMessage> RequestAsyncHandler<in T>(T self, RequestMessage request, Action<ResponseMessage> onCompleted);

    public class RequestHandlerItem<T>
    {
        public Type InterfaceType;
        public bool IsReentrant;
        public RequestHandler<T> Handler;
        public RequestAsyncHandler<T> AsyncHandler;
    }
}
