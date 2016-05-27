using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedActorRef
    {
        public IActorRef Actor { get; internal protected set; }
        public IRequestWaiter RequestWaiter { get; internal protected set; }
        public TimeSpan? Timeout { get; internal protected set; }

        protected InterfacedActorRef(IActorRef actor)
        {
            Actor = actor;
            RequestWaiter = AkkaAskRequestWaiter.Instance;
        }

        protected InterfacedActorRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout = null)
        {
            Actor = actor;
            RequestWaiter = requestWaiter;
            Timeout = timeout;
        }

        // Request & Response

        protected void SendRequest(RequestMessage requestMessage)
        {
            RequestWaiter.SendRequest(Actor, requestMessage);
        }

        protected Task SendRequestAndWait(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndWait(Actor, requestMessage, Timeout);
        }

        protected Task<TReturn> SendRequestAndReceive<TReturn>(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndReceive<TReturn>(Actor, requestMessage, Timeout);
        }
    }

    // Internal use only
    public static class InterfacedActorRefModifier
    {
        public static void SetActor(InterfacedActorRef a, IActorRef actor)
        {
            a.Actor = actor;
        }

        public static void SetRequestWaiter(InterfacedActorRef a, IRequestWaiter requestWaiter)
        {
            a.RequestWaiter = requestWaiter;
        }

        public static void SetTimeout(InterfacedActorRef a, TimeSpan? timeout)
        {
            a.Timeout = timeout;
        }
    }
}
