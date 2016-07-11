using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public class InterfacedMessageBuilder
    {
        private class SinkRequestWaiter : IRequestWaiter
        {
            public RequestMessage Message { get; private set; }

            void IRequestWaiter.SendRequest(IRequestTarget target, RequestMessage requestMessage)
            {
                Message = requestMessage;
            }

            Task IRequestWaiter.SendRequestAndWait(IRequestTarget target, RequestMessage requestMessage, TimeSpan? timeout)
            {
                Message = requestMessage;
                return Task.FromResult(0);
            }

            Task<TReturn> IRequestWaiter.SendRequestAndReceive<TReturn>(IRequestTarget target, RequestMessage requestMessage, TimeSpan? timeout)
            {
                Message = requestMessage;
                return Task.FromResult(default(TReturn));
            }
        }

        public static RequestMessage Request<T>(Action<T> action)
            where T : IInterfacedActor
        {
            var waiter = new SinkRequestWaiter();
            var actorRef = InterfacedActorRef.Create(typeof(T));
            InterfacedActorRefModifier.SetRequestWaiter(actorRef, waiter);
            action((T)(object)actorRef);
            return waiter.Message;
        }

        private class SinkNotificationChannel : INotificationChannel
        {
            public NotificationMessage Message { get; private set; }

            void INotificationChannel.Notify(NotificationMessage notificationMessage)
            {
                Message = notificationMessage;
            }
        }

        public static NotificationMessage Notification<T>(Action<T> action)
            where T : IInterfacedObserver
        {
            var channel = new SinkNotificationChannel();
            var observer = InterfacedObserver.Create(typeof(T));
            observer.Channel = channel;
            action((T)(object)observer);
            return channel.Message;
        }
    }
}
