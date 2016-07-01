using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    internal delegate void MessageHandler(object self, object message);
    internal delegate Task MessageAsyncHandler(object self, object message);

    internal class MessageHandlerItem
    {
        public bool IsReentrant;
        public MessageHandler Handler;
        public MessageAsyncHandler AsyncHandler;

        // for generic method, GenericHandlerBuilder will be used to construct the handler when parameter types are ready.
        public bool IsGeneric;
        public Func<Type, MessageHandlerItem> GenericHandlerBuilder;
    }
}
