namespace Akka.Interfaced
{
    public abstract class InterfacedObserver
    {
        public INotificationChannel Channel { get; set; }
        public int ObserverId { get; set; }

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
}
