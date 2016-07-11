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

    [Flags]
    public enum ActorBindingFlags
    {
        OpenThenNotification = 1,           // When channel open: Bound actor will be invoked a `IActorBoundChannelObserver.ChannelOpen` which has to be implemented.
        CloseThenDefault = (0 << 1),        // When channel closed: For a child, `CloseThenStop`. For others, `CloseThenNothing`.
        CloseThenNothing = (1 << 1),        // When channel closed: Do nothing
        CloseThenStop = (2 << 1),           // When channel closed: Bound actor will be sent a InterfacedPoisonPill.
        CloseThenNotification = (3 << 1),   // When channel closed: Bound actor will be invoked a `IActorBoundChannelObserver.ChannelClosed` which has to be implemented.
        StopThenCloseChannel = 8,           // If a bound actor stops, then close holding channel.
    }

    public interface IActorBoundChannel : IInterfacedActor
    {
        Task SetTag(object tag);
        Task<InterfacedActorRef> BindActor(InterfacedActorRef actor, ActorBindingFlags bindingFlags = 0);
        Task<IRequestTarget> BindActor(IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags = 0);
        Task<bool> UnbindActor(IActorRef actor);
        Task<bool> BindType(IActorRef actor, TaggedType[] types);
        Task<bool> UnbindType(IActorRef actor, Type[] types);
        Task Close();
    }

    public interface IActorBoundChannelObserver : IInterfacedObserver
    {
        void ChannelOpen(IActorBoundChannel channel, object tag);
        void ChannelOpenTimeout(object tag);
        void ChannelClose(IActorBoundChannel channel, object tag);
    }
}
