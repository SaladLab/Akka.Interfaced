using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public sealed class TaggedType
    {
        public Type Type { get; }
        public object TagValue { get; }

        public TaggedType(Type type, object tagValue = null)
        {
            Type = type;
            TagValue = tagValue;
        }

        public override bool Equals(object obj)
        {
            var t = obj as TaggedType;
            return (t != null) && (Type == t.Type && TagValue == t.TagValue);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public static implicit operator TaggedType(Type type)
        {
            return new TaggedType(type);
        }
    }

    public enum ChannelClosedNotificationType
    {
        Default = 0,                    // For a child, InterfacedPoisonPill. For others, Nothing.
        Nothing = 1,                    // Do nothing
        InterfacedPoisonPill = 2,       // Bound actor will be sent a InterfacedPoisonPill
        ChannelClosed = 3,              // Bound actor will be invoked a `IActorBoundChannelObserver.ChannelClosed` which has to be implemented.
    }

    public interface IActorBoundChannel : IInterfacedActor
    {
        Task<int> BindActor(IActorRef actor, TaggedType[] types, ChannelClosedNotificationType channelClosedNotification = ChannelClosedNotificationType.Default);
        Task<bool> UnbindActor(IActorRef actor);
        Task<bool> BindType(IActorRef actor, TaggedType[] types);
        Task<bool> UnbindType(IActorRef actor, Type[] types);
    }

    public interface IActorBoundChannelObserver : IInterfacedObserver
    {
        void ChannelClose(TaggedType[] types);
    }
}
