using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate void NotificationHandler<in T>(T self, NotificationMessage message);
    public delegate Task NotificationAsyncHandler<in T>(T self, NotificationMessage message);

    public class NotificationHandlerItem<T>
    {
        public Type InterfaceType;
        public bool IsReentrant;
        public NotificationHandler<T> Handler;
        public NotificationAsyncHandler<T> AsyncHandler;
    }
}
