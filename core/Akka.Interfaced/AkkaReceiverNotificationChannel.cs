using Akka.Actor;

namespace Akka.Interfaced
{
    public class AkkaReceiverNotificationChannel : INotificationChannel
    {
        public ICanTell Receiver { get; private set; }

        private int _lastNotificationId;

        public AkkaReceiverNotificationChannel(ICanTell receiver)
        {
            Receiver = receiver;
        }

        public void Notify(NotificationMessage notificationMessage)
        {
            var notificationId = ++_lastNotificationId;
            if (notificationId <= 0)
                notificationId = _lastNotificationId = 1;

            notificationMessage.NotificationId = notificationId;

            var sender = ActorCell.GetCurrentSelfOrNoSender();
            Receiver.Tell(notificationMessage, sender);
        }

        public override bool Equals(object obj)
        {
            var c = obj as AkkaReceiverNotificationChannel;
            if (c == null)
                return false;

            return Receiver.Equals(c.Receiver);
        }

        public override int GetHashCode()
        {
            return Receiver.GetHashCode();
        }

        public static bool OverrideReceiver(INotificationChannel channel, ICanTell receiver)
        {
            var akkaChannel = channel as AkkaReceiverNotificationChannel;
            if (akkaChannel != null)
            {
                akkaChannel.Receiver = receiver;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
