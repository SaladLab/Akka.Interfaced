using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedObserver
    {
        public INotificationChannel Channel { get; protected set; }
        public int ObserverId { get; protected set; }

        protected InterfacedObserver(INotificationChannel channel, int observerId)
        {
            Channel = channel;
            ObserverId = observerId;
        }

        protected void Notify(IInvokable message)
        {
            Channel.Notify(new NotificationMessage { ObserverId = ObserverId, InvokePayload = message });
        }

        public override bool Equals(object obj)
        {
            var o = obj as InterfacedObserver;
            if (o == null)
                return false;

            return Channel.Equals(o.Channel) && ObserverId == o.ObserverId;
        }

        public override int GetHashCode()
        {
            return (Channel.GetHashCode() * 17) + ObserverId;
        }
    }

    public class ActorNotificationChannel : INotificationChannel
    {
        public IActorRef Actor { get; }

        public ActorNotificationChannel(IActorRef actor)
        {
            Actor = actor;
        }

        public void Notify(NotificationMessage notificationMessage)
        {
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
