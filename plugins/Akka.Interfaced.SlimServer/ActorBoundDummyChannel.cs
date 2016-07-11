using System;
using System.Linq;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    // DummyChannel just pretends to work as the actor-bound channel.
    public class ActorBoundDummyChannel : ActorBoundChannelBase, IActorBoundChannelSync
    {
        public ActorBoundDummyChannel()
        {
        }

        public ActorBoundDummyChannel(Tuple<IActorRef, TaggedType[], ActorBindingFlags> bindingActor)
        {
            BindActor(bindingActor.Item1, bindingActor.Item2.Select(t => new BoundType(t)), bindingActor.Item3);
        }

        protected override void OnResponseMessage(ResponseMessage message)
        {
        }

        protected override void OnNotificationMessage(NotificationMessage message)
        {
        }

        protected override void OnCloseRequest()
        {
            Close();
        }

        [ResponsiveExceptionAll]
        InterfacedActorRef IActorBoundChannelSync.BindActor(InterfacedActorRef actor, ActorBindingFlags bindingFlags)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            var targetActor = actor.CastToIActorRef();
            if (targetActor == null)
                throw new ArgumentException("InterfacedActorRef should have valid IActorRef target.");

            var actorId = BindActor(targetActor, new[] { new BoundType(actor.InterfaceType) }, bindingFlags);
            if (actorId == 0)
                return null;

            return actor;
        }

        [ResponsiveExceptionAll]
        IRequestTarget IActorBoundChannelSync.BindActor(IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            var actorId = BindActor(actor, types.Select(t => new BoundType(t)), bindingFlags);
            return actorId != 0 ? new AkkaReceiverTarget(actor) : null;
        }
    }
}
