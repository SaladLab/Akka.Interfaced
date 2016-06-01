namespace Akka.Interfaced
{
    public class ObjectNotificationChannel : INotificationChannel
    {
        public IInterfacedObserver Observer { get; }

        public ObjectNotificationChannel(IInterfacedObserver observer)
        {
            Observer = observer;
        }

        public void Notify(NotificationMessage notificationMessage)
        {
            notificationMessage.InvokePayload.Invoke(Observer);
        }

        public override bool Equals(object obj)
        {
            var c = obj as ObjectNotificationChannel;
            if (c == null)
                return false;

            return Observer.Equals(c.Observer);
        }

        public override int GetHashCode()
        {
            return Observer.GetHashCode();
        }

        public static TObserver Create<TObserver>(TObserver observer)
            where TObserver : IInterfacedObserver
        {
            var proxy = InterfacedObserver.Create(typeof(TObserver));
            proxy.Channel = new ObjectNotificationChannel(observer);
            return (TObserver)(object)proxy;
        }
    }
}
