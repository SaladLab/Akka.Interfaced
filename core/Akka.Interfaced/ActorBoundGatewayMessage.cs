using System;
using Akka.Actor;

namespace Akka.Interfaced
{
    public static class ActorBoundGatewayMessage
    {
        // Open a channel with a bound actor.
        public class Open
        {
            public IActorRef Actor { get; }
            public ActorBoundChannelMessage.InterfaceType[] Types { get; }

            public Open(IActorRef actor, Type type, object tagValue = null)
                : this(actor, new ActorBoundChannelMessage.InterfaceType(type, tagValue))
            {
            }

            public Open(IActorRef actor, ActorBoundChannelMessage.InterfaceType type)
            {
                Actor = actor;
                Types = new[] { type };
            }

            public Open(IActorRef actor, ActorBoundChannelMessage.InterfaceType[] types)
            {
                Actor = actor;
                Types = types;
            }
        }

        public class OpenReply
        {
            // Address consisting of EndPoint and Token used by SlimClient to establish new channel.
            public string Address;

            public OpenReply(string address)
            {
                Address = address;
            }
        }
    }
}
