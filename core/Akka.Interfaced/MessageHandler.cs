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
    }
}
