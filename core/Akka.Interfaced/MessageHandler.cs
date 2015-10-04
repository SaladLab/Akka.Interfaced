using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate void MessageHandler<in T>(T self, object message);
    public delegate Task MessageAsyncHandler<in T>(T self, object message);

    public class MessageHandlerItem<T>
    {
        public bool IsReentrant;
        public MessageHandler<T> Handler;
        public MessageAsyncHandler<T> AsyncHandler;
    }
}
