using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedObserver
    {
        public INotificationChannel Channel { get; protected set; }
        public int ObserverId { get; protected set; }

        public InterfacedObserver(INotificationChannel channel, int observerId)
        {
            Channel = channel;
            ObserverId = observerId;
        }

        protected void Notify(IInvokable message)
        {
            Channel.Notify(new NotificationMessage {ObserverId = ObserverId, Message = message});
        }

        // TODO: Subscribe / Unsubscribe 때 객체가 달라도 논리적으로 올바르게 동작하려면
        //       Hash, Equal 을 override 해야 한다
    }

    public class ActorNotificationChannel : INotificationChannel
    {
        public IActorRef Actor { get; private set; }

        public ActorNotificationChannel(IActorRef actor)
        {
            Actor = actor;
        }

        public void Notify(NotificationMessage notificationMessage)
        {
            Actor.Tell(notificationMessage);
        }
    }
}
