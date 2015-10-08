using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public abstract class InterfacedSlimActorRef
    {
        public ISlimActorRef Actor { get; protected set; }
        public ISlimRequestWaiter RequestWaiter { get; protected set; }
        public TimeSpan? Timeout { get; protected set; }

        public InterfacedSlimActorRef(ISlimActorRef actor, ISlimRequestWaiter requestWaiter, TimeSpan? timeout = null)
        {
            Actor = actor;
            RequestWaiter = requestWaiter;
            Timeout = timeout;
        }

        protected void SendRequest(SlimRequestMessage requestMessage)
        {
            RequestWaiter.SendRequest(Actor, requestMessage);
        }

        protected Task SendRequestAndWait(SlimRequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndWait(Actor, requestMessage, Timeout);
        }

        protected Task<TReturn> SendRequestAndReceive<TReturn>(SlimRequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndReceive<TReturn>(Actor, requestMessage, Timeout);
        }
    }
}
