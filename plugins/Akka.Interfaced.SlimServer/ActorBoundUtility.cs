using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public static class ActorBoundUtility
    {
        public static Task<BoundActorTarget> BindActorOrOpenChannel(
            this ActorBoundChannelRef channel, IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags,
            string gatewayName, object tag)
        {
            return BindActorOrOpenChannel(channel, actor, types, bindingFlags, gatewayName, tag, bindingFlags);
        }

        public static async Task<BoundActorTarget> BindActorOrOpenChannel(
            this ActorBoundChannelRef channel, IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags,
            string gatewayName, object tag, ActorBindingFlags bindingFlagsForOpenChannel)
        {
            if (channel != null && channel.CastToIActorRef().Path.Address == actor.Path.Address)
            {
                // link an actor to channel directly
                return await channel.BindActor(actor, types, bindingFlags);
            }
            else
            {
                // grant client to access an actor via gateway
                var gatewayRef = ((InternalActorRefBase)actor).Provider.ResolveActorRef(actor.Path.Root / "user" / gatewayName);
                var gateway = new ActorBoundGatewayRef(new AkkaReceiverTarget(gatewayRef));
                return await gateway.WithTimeout(TimeSpan.FromSeconds(10)).OpenChannel(actor, types, tag, bindingFlagsForOpenChannel);
            }
        }
    }
}
