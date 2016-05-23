using Akka.Actor;

namespace Akka.Interfaced
{
    public class ActorNotificationChannel : INotificationChannel
    {
        public IActorRef Actor { get; }

        private int _lastNotificationId;

        public ActorNotificationChannel(IActorRef actor)
        {
            Actor = actor;
        }

        public void Notify(NotificationMessage notificationMessage)
        {
            var notificationId = ++_lastNotificationId;
            if (notificationId <= 0)
                notificationId = _lastNotificationId = 1;

            notificationMessage.NotificationId = notificationId;
            Actor.Tell(notificationMessage);
        }

        public override bool Equals(object obj)
        {
            var c = obj as ActorNotificationChannel;
            if (c == null)
                return false;

            return Actor.Equals(c.Actor);
        }

        public override int GetHashCode()
        {
            return Actor.GetHashCode();
        }
    }
}
