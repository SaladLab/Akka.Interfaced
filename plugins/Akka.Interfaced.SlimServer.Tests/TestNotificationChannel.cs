using System.Collections.Generic;

namespace Akka.Interfaced.SlimServer
{
    public class TestNotificationChannel : INotificationChannel
    {
        public IInterfacedObserver Observer { get; set; }
        public IList<NotificationMessage> Messages { get; set; }

        void INotificationChannel.Notify(NotificationMessage notificationMessage)
        {
            if (Observer != null)
                notificationMessage.InvokePayload.Invoke(Observer);

            if (Messages != null)
                Messages.Add(notificationMessage);
        }
    }
}
