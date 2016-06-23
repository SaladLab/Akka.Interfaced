namespace Akka.Interfaced
{
    public static class InterfacedActorRefExtensions
    {
        // Cast (not type-safe)

        public static TRef Cast<TRef>(this InterfacedActorRef actorRef)
            where TRef : InterfacedActorRef, new()
        {
            if (actorRef == null)
                return null;

            return new TRef()
            {
                Target = actorRef.Target,
                RequestWaiter = actorRef.RequestWaiter,
                Timeout = actorRef.Timeout
            };
        }

        // Wrap target into TRef (not type-safe)

        public static TRef Cast<TRef>(this BoundActorTarget target)
            where TRef : InterfacedActorRef, new()
        {
            if (target == null)
                return null;

            return new TRef()
            {
                Target = target,
                RequestWaiter = target.DefaultRequestWaiter
            };
        }
    }
}
