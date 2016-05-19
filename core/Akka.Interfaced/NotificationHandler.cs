using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate void NotificationHandler(object self, NotificationMessage message);
    public delegate Task NotificationAsyncHandler(object self, NotificationMessage message);

    public class NotificationHandlerItem
    {
        public Type InterfaceType;
        public bool IsReentrant;
        public NotificationHandler Handler;
        public NotificationAsyncHandler AsyncHandler;
    }
}
