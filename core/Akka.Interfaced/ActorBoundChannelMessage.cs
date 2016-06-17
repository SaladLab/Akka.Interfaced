using System;
using Akka.Actor;

namespace Akka.Interfaced
{
    public static class ActorBoundChannelMessage
    {
        public class InterfaceType
        {
            public Type Type { get; }
            public object TagValue { get; }

            public InterfaceType(Type type, object tagValue = null)
            {
                Type = type;
                TagValue = tagValue;
            }
        }

        // Bind an (typed) actor to a destinated channel
        public class Bind
        {
            public IActorRef Actor { get; }
            public InterfaceType[] Types { get; }

            public Bind(IActorRef actor, Type type, object tagValue = null)
                : this(actor, new InterfaceType(type, tagValue))
            {
            }

            public Bind(IActorRef actor, InterfaceType type)
            {
                Actor = actor;
                Types = new[] { type };
            }

            public Bind(IActorRef actor, InterfaceType[] types)
            {
                Actor = actor;
                Types = types;
            }
        }

        public class BindReply
        {
            public int ActorId;

            public BindReply(int actorId)
            {
                ActorId = actorId;
            }
        }

        // Unbind an actor to a destinated channel
        public class Unbind
        {
            public IActorRef Actor;

            public Unbind(IActorRef actor)
            {
                Actor = actor;
            }
        }

        // Add more types to a bound actor.
        public class AddType
        {
            public IActorRef Actor { get; }
            public InterfaceType[] Types { get; }

            public AddType(IActorRef actor, Type type, object tagValue = null)
                : this(actor, new InterfaceType(type, tagValue))
            {
            }

            public AddType(IActorRef actor, InterfaceType type)
            {
                Actor = actor;
                Types = new[] { type };
            }

            public AddType(IActorRef actor, InterfaceType[] types)
            {
                Actor = actor;
                Types = types;
            }
        }

        // Remove (allowed) types to a bound actor.
        public class RemoveType
        {
            public IActorRef Actor { get; }
            public Type[] Types { get; }

            public RemoveType(IActorRef actor, Type type)
            {
                Actor = actor;
                Types = new[] { type };
            }

            public RemoveType(IActorRef actor, Type[] types)
            {
                Actor = actor;
                Types = types;
            }
        }

        public class ChannelTerminated
        {
        }
    }
}
