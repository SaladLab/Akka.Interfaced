using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public abstract class ActorBoundChannelBase : InterfacedActor, IActorBoundChannelSync
    {
        protected class BoundActor
        {
            public IActorRef Actor;
            public bool IsChildActor;
            public ChannelClosedNotificationType ChannelClosedNotification;
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

            public BoundType(TaggedType type)
            {
                Type = type.Type;
                TagValue = type.TagValue;
            }
        }

        private readonly object _boundActorLock = new object();
        private readonly Dictionary<int, BoundActor> _boundActorMap = new Dictionary<int, BoundActor>();
        private readonly Dictionary<IActorRef, int> _boundActorInverseMap = new Dictionary<IActorRef, int>();
        private int _lastBoundActorId;
        private bool _closed;

        // When OnResponseMessage received
        protected abstract void OnResponseMessage(ResponseMessage message);

        // When NotificationMessage received
        protected abstract void OnNotificationMessage(NotificationMessage message);

        // Close channel and send channel-closed notification to bound actors.
        protected void Close()
        {
            if (_closed)
                return;

            _closed = true;

            // Send channel-closed notification message to bound actors

            lock (_boundActorLock)
            {
                foreach (var i in _boundActorMap)
                {
                    var notification = i.Value.ChannelClosedNotification;
                    if (notification == ChannelClosedNotificationType.Default)
                    {
                        notification = i.Value.IsChildActor
                            ? ChannelClosedNotificationType.InterfacedPoisonPill
                            : ChannelClosedNotificationType.Nothing;
                    }

                    switch (notification)
                    {
                        case ChannelClosedNotificationType.InterfacedPoisonPill:
                            i.Value.Actor.Tell(InterfacedPoisonPill.Instance);
                            break;

                        case ChannelClosedNotificationType.ChannelClosed:
                            i.Value.Actor.Tell(new NotificationMessage
                            {
                                InvokePayload = new IActorBoundChannelObserver_PayloadTable.ChannelClose_Invoke
                                {
                                    types = i.Value.Types.Select(t => new TaggedType(t.Type, t.TagValue)).ToArray()
                                },
                            });
                            break;
                    }
                }

                // after sending notification, waiting for all children to stop.
                // but in this case, there is no child so stop now.

                if (_boundActorMap.Any(i => i.Value.IsChildActor) == false)
                    Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        private void OnChildTerminate(Terminated m)
        {
            lock (_boundActorLock)
            {
                UnbindActor(m.ActorRef);

                // all children stopped and it's time to stop self now

                if (_closed && _boundActorMap.Any(i => i.Value.IsChildActor) == false)
                    Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        protected override void PostStop()
        {
            if (_closed == false)
            {
                Context.System.EventStream.Publish(new Event.Warning(
                    Self.Path.ToString(), GetType(),
                    $"ActorBoundChannel should be called Close before Stop."));
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

            var terminated = message as Terminated;
            if (terminated != null)
            {
                OnChildTerminate(terminated);
                return;
            }

            base.OnReceive(message);
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

        protected int BindActor(IActorRef actor, IEnumerable<BoundType> boundTypes, ChannelClosedNotificationType channelClosedNotification = ChannelClosedNotificationType.Nothing)
        {
            if (_closed)
                return 0;

            lock (_boundActorLock)
            {
                if (GetBoundActorId(actor) != 0)
                    return 0;

                var actorId = ++_lastBoundActorId;
                var boundActor = new BoundActor
                {
                    Actor = actor,
                    IsChildActor = (Self.Path == actor.Path.Parent),
                    ChannelClosedNotification = channelClosedNotification,
                    Types = boundTypes.ToList(),
                    DerivedTypes = GetDerivedBoundTypes(boundTypes)
                };
                _boundActorMap[actorId] = boundActor;
                _boundActorInverseMap[actor] = actorId;

                // watch this actor if child.
                if (boundActor.IsChildActor)
                    Context.Watch(actor);

                return actorId;
            }
        }

        protected bool UnbindActor(IActorRef actor)
        {
            lock (_boundActorLock)
            {
                int actorId;
                if (_boundActorInverseMap.TryGetValue(actor, out actorId))
                {
                    var boundActor = _boundActorMap[actorId];
                    _boundActorMap.Remove(actorId);
                    _boundActorInverseMap.Remove(actor);

                    // unwatch this actor if child.
                    if (_closed == false && boundActor.IsChildActor)
                        Context.Unwatch(actor);

                    return true;
                }
            }
            return false;
        }

        protected bool BindType(IActorRef actor, IEnumerable<BoundType> boundTypes)
        {
            if (_closed)
                return false;

            lock (_boundActorLock)
            {
                var actorId = GetBoundActorId(actor);
                if (actorId == 0)
                    return false;

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
                return true;
            }
        }

        protected bool UnbindType(IActorRef actor, IEnumerable<Type> types)
        {
            if (_closed)
                return false;

            lock (_boundActorLock)
            {
                var actorId = GetBoundActorId(actor);
                if (actorId == 0)
                    return false;

                var boundActor = _boundActorMap[actorId];
                foreach (var type in types)
                {
                    boundActor.Types.RemoveAll(t => t.Type == type);
                }
                boundActor.DerivedTypes = GetDerivedBoundTypes(boundActor.Types);
                return true;
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

        [ResponsiveExceptionAll]
        int IActorBoundChannelSync.BindActor(IActorRef actor, TaggedType[] types, ChannelClosedNotificationType channelClosedNotification)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            var actorId = BindActor(actor, types.Select(t => new BoundType(t)), channelClosedNotification);
            return actorId;
        }

        [ResponsiveExceptionAll]
        bool IActorBoundChannelSync.UnbindActor(IActorRef actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            return UnbindActor(actor);
        }

        [ResponsiveExceptionAll]
        bool IActorBoundChannelSync.BindType(IActorRef actor, TaggedType[] types)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            return BindType(actor, types.Select(t => new BoundType(t)));
        }

        [ResponsiveExceptionAll]
        bool IActorBoundChannelSync.UnbindType(IActorRef actor, Type[] types)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            return UnbindType(actor, types);
        }
    }
}
