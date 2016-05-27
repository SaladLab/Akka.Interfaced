using System;
using Akka.Actor;
using Akka.Util;

namespace Akka.Interfaced
{
    // For transfering the InterfacedRef to slim-client.
    public class BoundActorRef : ActorRefBase
    {
        public override ActorPath Path => null;

        public int Id { get; set; }

        public BoundActorRef(int id)
        {
            Id = id;
        }

        public override ISurrogate ToSurrogate(ActorSystem system)
        {
            return null;
        }

        protected override void TellInternal(object message, IActorRef sender)
        {
            throw new InvalidOperationException("ActorBoundProxyActorRef cannot tell.");
        }

        public static TRef Create<TRef>(int boundActorId)
            where TRef : InterfacedActorRef, new()
        {
            var a = new TRef();
            InterfacedActorRefModifier.SetActor(a, new BoundActorRef(boundActorId));
            return a;
        }
    }
}
