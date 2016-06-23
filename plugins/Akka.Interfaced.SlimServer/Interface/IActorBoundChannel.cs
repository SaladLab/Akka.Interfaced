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
        CloseThenDefault = 0,           // When channel closed: For a child, `CloseThenStop`. For others, `CloseThenNothing`.
        CloseThenNothing = 1,           // When channel closed: Do nothing
        CloseThenStop = 2,              // When channel closed: Bound actor will be sent a InterfacedPoisonPill.
        CloseThenNotification = 3,      // When channel closed: Bound actor will be invoked a `IActorBoundChannelObserver.ChannelClosed` which has to be implemented.
        StopThenCloseChannel = 4,       // If bound actor stops, then close holding channel.
    }

    public interface IActorBoundChannel : IInterfacedActor
    {
        Task<int> BindActor(InterfacedActorRef actor, ActorBindingFlags bindingFlags = 0);
        Task<int> BindActor(IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags = 0);
        Task<bool> UnbindActor(IActorRef actor);
        Task<bool> BindType(IActorRef actor, TaggedType[] types);
        Task<bool> UnbindType(IActorRef actor, Type[] types);
    }

    public interface IActorBoundChannelObserver : IInterfacedObserver
    {
        void ChannelClose(TaggedType[] types);
    }
}
