using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    internal delegate void NotificationHandler(object self, NotificationMessage message);
    internal delegate Task NotificationAsyncHandler(object self, NotificationMessage message);

    internal class NotificationHandlerItem
    {
        public Type InterfaceType;
        public bool IsReentrant;
        public NotificationHandler Handler;
        public NotificationAsyncHandler AsyncHandler;
    }
}
