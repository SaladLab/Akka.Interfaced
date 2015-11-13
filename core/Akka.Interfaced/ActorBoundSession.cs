using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class ActorBoundSession : UntypedActor
    {
        protected class BoundActor
        {
            public IActorRef Actor;
            public bool IsChildActor;
            public Type InterfaceType;
            public bool IsTagOverridable;
            public object TagValue;
        }

        private readonly object _boundActorLock = new object();
        private readonly Dictionary<int, BoundActor> _boundActorMap = new Dictionary<int, BoundActor>();
        private readonly Dictionary<IActorRef, int> _boundActorInverseMap = new Dictionary<IActorRef, int>();
        private int _lastBoundActorId;

        // When NotificationMessage received
        protected abstract void OnNotificationMessage(NotificationMessage message);

        // When OnResponseMessage received
        protected abstract void OnResponseMessage(ResponseMessage message);

        protected override void PostStop()
        {
            lock (_boundActorLock)
            {
                foreach (var a in _boundActorMap.Values)
                {
                    if (a.IsChildActor == false)
                        a.Actor.Tell(new ActorBoundSessionMessage.SessionTerminated());
                }

                _boundActorMap.Clear();
            }
        }

        protected override void OnReceive(object message)
        {
            var notificationMessage = message as NotificationMessage;
            if (notificationMessage != null)
            {
                OnNotificationMessage(notificationMessage);
                return;
            }

            var responseMessage = message as ResponseMessage;
            if (responseMessage != null)
            {
                OnResponseMessage(responseMessage);
                return;
            }

            var bindMessage = message as ActorBoundSessionMessage.Bind;
            if (bindMessage != null)
            {
                var actorId = BindActor(bindMessage.Actor, bindMessage.InterfaceType, bindMessage.TagValue);
                Sender.Tell(new ActorBoundSessionMessage.BindReply(actorId));
                return;
            }

            var unbindMessage = message as ActorBoundSessionMessage.Unbind;
            if (unbindMessage != null)
            {
                if (unbindMessage.Actor != null)
                    UnbindActor(unbindMessage.Actor);
                else if (unbindMessage.ActorId != 0)
                    UnbindActor(unbindMessage.ActorId);
                return;
            }
        }

        protected int BindActor(IActorRef actor, Type interfaceType, object tagValue = null)
        {
            lock (_boundActorLock)
            {
                var actorId = ++_lastBoundActorId;
                _boundActorMap[actorId] = new BoundActor
                {
                    Actor = actor,
                    IsChildActor = (Self.Path == actor.Path.Parent),
                    InterfaceType = interfaceType,
                    IsTagOverridable = interfaceType?.GetCustomAttribute<TagOverridableAttribute>() != null,
                    TagValue = tagValue
                };
                _boundActorInverseMap[actor] = actorId;
                return actorId;
            }
        }

        protected BoundActor GetBoundActor(int id)
        {
            lock (_boundActorLock)
            {
                BoundActor item;
                return _boundActorMap.TryGetValue(id, out item) ? item : null;
            }
        }

        protected int GetBoundActorId(IActorRef actor)
        {
            lock (_boundActorLock)
            {
                int actorId;
                return _boundActorInverseMap.TryGetValue(actor, out actorId) ? actorId : 0;
            }
        }

        protected void UnbindActor(IActorRef actor)
        {
            lock (_boundActorLock)
            {
                int actorId;
                if (_boundActorInverseMap.TryGetValue(actor, out actorId))
                {
                    _boundActorMap.Remove(actorId);
                    _boundActorInverseMap.Remove(actor);
                }
            }
        }

        protected void UnbindActor(int actorId)
        {
            lock (_boundActorLock)
            {
                BoundActor item;
                if (_boundActorMap.TryGetValue(actorId, out item))
                {
                    _boundActorMap.Remove(actorId);
                    _boundActorInverseMap.Remove(item.Actor);
                }
            }
        }
    }
}
