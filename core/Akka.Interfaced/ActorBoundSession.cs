using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class ActorBoundSession : UntypedActor
    {
        protected class BoundActor
        {
            public IActorRef Actor;
            public bool IsChildActor;
            public List<BoundType> Types;
            public List<BoundType> DerivedTypes;

            public BoundType FindBoundType(Type type)
            {
                return DerivedTypes.FirstOrDefault(b => b.Type == type);
            }
        }

        protected class BoundType
        {
            public Type Type;
            public bool IsTagOverridable;
            public object TagValue;

            public BoundType()
            {
            }

            public BoundType(ActorBoundSessionMessage.InterfaceType type)
            {
                Type = type.Type;
                TagValue = type.TagValue;
            }
        }

        private readonly object _boundActorLock = new object();
        private readonly Dictionary<int, BoundActor> _boundActorMap = new Dictionary<int, BoundActor>();
        private readonly Dictionary<IActorRef, int> _boundActorInverseMap = new Dictionary<IActorRef, int>();
        private int _lastBoundActorId;

        // When OnResponseMessage received
        protected abstract void OnResponseMessage(ResponseMessage message);

        // When NotificationMessage received
        protected abstract void OnNotificationMessage(NotificationMessage message);

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
            // Messages from bound actors

            var responseMessage = message as ResponseMessage;
            if (responseMessage != null)
            {
                OnResponseMessage(responseMessage);
                return;
            }

            var notificationMessage = message as NotificationMessage;
            if (notificationMessage != null)
            {
                OnNotificationMessage(notificationMessage);
                return;
            }

            // ActorBound messages

            var bindMessage = message as ActorBoundSessionMessage.Bind;
            if (bindMessage != null)
            {
                var actorId = BindActor(
                    bindMessage.Actor,
                    bindMessage.Types.Select(t => new BoundType(t)));
                Sender.Tell(new ActorBoundSessionMessage.BindReply(actorId));
                return;
            }

            var unbindMessage = message as ActorBoundSessionMessage.Unbind;
            if (unbindMessage != null)
            {
                if (unbindMessage.Actor != null)
                    UnbindActor(unbindMessage.Actor);
                return;
            }

            var addTypeMessage = message as ActorBoundSessionMessage.AddType;
            if (addTypeMessage != null)
            {
                AddType(
                    addTypeMessage.Actor,
                    addTypeMessage.Types.Select(t => new BoundType(t)));
                return;
            }

            var removeTypeMessage = message as ActorBoundSessionMessage.RemoveType;
            if (removeTypeMessage != null)
            {
                RemoveType(
                    removeTypeMessage.Actor,
                    removeTypeMessage.Types);
                return;
            }

            Unhandled(message);
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

        protected int BindActor(IActorRef actor, IEnumerable<BoundType> boundTypes)
        {
            lock (_boundActorLock)
            {
                if (GetBoundActorId(actor) != 0)
                    return 0;

                var actorId = ++_lastBoundActorId;
                _boundActorMap[actorId] = new BoundActor
                {
                    Actor = actor,
                    IsChildActor = (Self.Path == actor.Path.Parent),
                    Types = boundTypes.ToList(),
                    DerivedTypes = GetDerivedBoundTypes(boundTypes)
                };
                _boundActorInverseMap[actor] = actorId;
                return actorId;
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

        protected void AddType(IActorRef actor, IEnumerable<BoundType> boundTypes)
        {
            lock (_boundActorLock)
            {
                var actorId = GetBoundActorId(actor);
                if (actorId == 0)
                    return;

                var boundActor = _boundActorMap[actorId];
                foreach (var bt in boundTypes)
                {
                    // upsert
                    var found = boundActor.Types.Find(t => t.Type == bt.Type);
                    if (found != null)
                        found.TagValue = bt.TagValue;
                    else
                        boundActor.Types.Add(bt);
                }
                boundActor.DerivedTypes = GetDerivedBoundTypes(boundActor.Types);
            }
        }

        protected void RemoveType(IActorRef actor, IEnumerable<Type> types)
        {
            lock (_boundActorLock)
            {
                var actorId = GetBoundActorId(actor);
                if (actorId == 0)
                    return;

                var boundActor = _boundActorMap[actorId];
                foreach (var type in types)
                {
                    boundActor.Types.RemoveAll(t => t.Type == type);
                }
                boundActor.DerivedTypes = GetDerivedBoundTypes(boundActor.Types);
            }
        }

        private static List<BoundType> GetDerivedBoundTypes(IEnumerable<BoundType> boundTypes)
        {
            var derivedBoundTypes = new List<BoundType>();
            foreach (var bt in boundTypes)
            {
                var baseTypes = bt.Type.GetInterfaces().Where(t => t != typeof(IInterfacedActor));
                foreach (var type in new[] { bt.Type }.Concat(baseTypes))
                {
                    derivedBoundTypes.Add(new BoundType
                    {
                        Type = type,
                        IsTagOverridable = type.GetCustomAttribute<TagOverridableAttribute>() != null,
                        TagValue = bt.TagValue
                    });
                }
            }
            return derivedBoundTypes;
        }
    }
}
