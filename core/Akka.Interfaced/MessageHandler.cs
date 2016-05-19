using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate void MessageHandler(object self, object message);
    public delegate Task MessageAsyncHandler(object self, object message);

    public class MessageHandlerItem
    {
        public bool IsReentrant;
        public MessageHandler Handler;
        public MessageAsyncHandler AsyncHandler;
    }
}
