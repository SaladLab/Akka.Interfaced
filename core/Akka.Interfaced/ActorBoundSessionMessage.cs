using System;
using Akka.Actor;

namespace Akka.Interfaced
{
    public static class ActorBoundSessionMessage
    {
        public class Bind
        {
            public IActorRef Actor { get; }
            public Type InterfaceType { get; }
            public object TagValue { get; }

            public Bind(IActorRef actor, Type interfaceType, object tagValue)
            {
                Actor = actor;
                InterfaceType = interfaceType;
                TagValue = tagValue;
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
            public int ActorId;

            public Unbind(IActorRef actor)
            {
                Actor = actor;
            }

            public Unbind(int actorId)
            {
                ActorId = actorId;
            }
        }

        public class SessionTerminated
        {
        }
    }
}
