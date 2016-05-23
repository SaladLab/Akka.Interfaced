using Xunit;

namespace Akka.Interfaced.SlimClient.Tests
{
    public class SlimObserver
    {
        [Fact]
        public void FireEvent_TransferedViaNotificationChannel()
        {
            var channel = new TestNotificationChannel();
            var o = new SubjectObserver(channel, 10);
            o.Event("Event");

            Assert.Equal(1, channel.Notifications.Count);
            Assert.Equal(10, channel.Notifications[0].ObserverId);
            var notification = (ISubjectObserver_PayloadTable.Event_Invoke)channel.Notifications[0].InvokePayload;
            Assert.NotNull(notification);
            Assert.Equal("Event", notification.eventName);
        }
    }
}
