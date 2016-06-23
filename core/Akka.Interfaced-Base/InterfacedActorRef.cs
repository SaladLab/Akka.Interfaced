using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public abstract class InterfacedActorRef
    {
        public IRequestTarget Target { get; protected internal set; }
        public IRequestWaiter RequestWaiter { get; protected internal set; }
        public TimeSpan? Timeout { get; protected internal set; }

        abstract public Type InterfaceType { get; }

        protected InterfacedActorRef(IRequestTarget target)
        {
            Target = target;
            RequestWaiter = target?.DefaultRequestWaiter;
        }

        protected InterfacedActorRef(IRequestTarget target, IRequestWaiter requestWaiter, TimeSpan? timeout = null)
        {
            Target = target;
            RequestWaiter = requestWaiter;
            Timeout = timeout;
        }

        // Request & Response

        protected void SendRequest(RequestMessage requestMessage)
        {
            RequestWaiter.SendRequest(Target, requestMessage);
        }

        protected Task SendRequestAndWait(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndWait(Target, requestMessage, Timeout);
        }

        protected Task<TReturn> SendRequestAndReceive<TReturn>(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndReceive<TReturn>(Target, requestMessage, Timeout);
        }

        // Cast (not type-safe cast)

        public TRef Cast<TRef>()
            where TRef : InterfacedActorRef, new()
        {
            return new TRef()
            {
                Target = Target,
                RequestWaiter = RequestWaiter,
                Timeout = Timeout
            };
        }
    }

    // Internal use only
    public static class InterfacedActorRefModifier
    {
        public static void SetTarget(InterfacedActorRef actorRef, IRequestTarget target)
        {
            actorRef.Target = target;
        }

        public static void SetRequestWaiter(InterfacedActorRef actorRef, IRequestWaiter requestWaiter)
        {
            actorRef.RequestWaiter = requestWaiter;
        }

        public static void SetTimeout(InterfacedActorRef actorRef, TimeSpan? timeout)
        {
            actorRef.Timeout = timeout;
        }
    }
}
