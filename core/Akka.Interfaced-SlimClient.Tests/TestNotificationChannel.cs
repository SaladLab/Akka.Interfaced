using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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
