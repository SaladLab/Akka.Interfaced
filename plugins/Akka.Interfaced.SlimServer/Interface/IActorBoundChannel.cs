using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public class TaggedType
    {
        public Type Type { get; }
        public object TagValue { get; }

        public TaggedType(Type type, object tagValue = null)
        {
            Type = type;
            TagValue = tagValue;
        }

        public static implicit operator TaggedType(Type type)
        {
            return new TaggedType(type);
        }
    }

    public enum ChannelClosedNotificationType
    {
        Default = 0,
        InterfacedPoisonPill = 1,
        ChannelClosed = 2,
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
