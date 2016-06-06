using System;
using Akka.Actor;

namespace Akka.Interfaced
{
    public static class ActorBoundSessionMessage
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

        public class Unbind
        {
            public IActorRef Actor;

            public Unbind(IActorRef actor)
            {
                Actor = actor;
            }
        }

        public class AddType
        {
            public IActorRef Actor { get; }
            public InterfaceType[] Types { get; }

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

        public class SessionTerminated
        {
        }
    }
}
