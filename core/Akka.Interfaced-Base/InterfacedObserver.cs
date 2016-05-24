using System;

namespace Akka.Interfaced
{
    public abstract class InterfacedObserver : IDisposable
    {
        public INotificationChannel Channel { get; set; }
        public int ObserverId { get; set; }
        public Action Disposed { get; set; }

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

        public void Dispose()
        {
            Dispose(true);
            Channel = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Disposed != null)
                {
                    Disposed();
                    Disposed = null;
                }
            }
        }

        public static InterfacedObserver Create(Type type)
        {
            if (type.IsInterface)
            {
                // Namespace.IExampleObserver -> Namespace.ExampleObserver
                var proxyTypeName = type.FullName.Substring(0, type.FullName.Length - type.Name.Length) + type.Name.Substring(1);
                var proxyType = type.Assembly.GetType(proxyTypeName);
                if (proxyType == null || proxyType.BaseType != typeof(InterfacedObserver))
                    throw new ArgumentException("Cannot resolve the observer type from " + type.FullName);

                var proxy = Activator.CreateInstance(proxyType);
                return (InterfacedObserver)proxy;
            }
            else if (type.IsClass)
            {
                // Namespace.ExampleObserver
                if (type.BaseType != typeof(InterfacedObserver))
                    throw new ArgumentException("Cannot create observer with " + type.FullName);

                var proxy = Activator.CreateInstance(type);
                return (InterfacedObserver)proxy;
            }
            else
            {
                throw new ArgumentException("Cannot create observer from " + type.FullName);
            }
        }
    }
}
