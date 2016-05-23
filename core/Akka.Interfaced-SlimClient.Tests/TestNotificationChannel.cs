using System.Collections.Generic;

namespace Akka.Interfaced.SlimClient.Tests
{
    public class TestNotificationChannel : INotificationChannel
    {
        public List<NotificationMessage> Notifications = new List<NotificationMessage>();

        public void Notify(NotificationMessage notificationMessage)
        {
            Notifications.Add(notificationMessage);
        }
    }
}
