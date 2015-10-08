using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedActorRef
    {
        public IActorRef Actor { get; protected set; }
        public IRequestWaiter RequestWaiter { get; protected set; }
        public TimeSpan? Timeout { get; protected set; }

        protected InterfacedActorRef(IActorRef actor)
        {
            Actor = actor;
            RequestWaiter = DefaultRequestWaiter.Instance;
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
            Actor.Tell(requestMessage);
        }

        protected Task SendRequestAndWait(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndWait(Actor, requestMessage, Timeout);
        }

        protected Task<T> SendRequestAndReceive<T>(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndReceive<T>(Actor, requestMessage, Timeout);
        }
    }

    internal class DefaultRequestWaiter : IRequestWaiter
    {
        Task IRequestWaiter.SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            requestMessage.RequestId = -1;
            return target.Ask<ResponseMessage>(requestMessage, timeout).ContinueWith(t =>
            {
                var response = t.Result;
                if (response.Exception != null)
                {
                    throw response.Exception;
                }
            });
        }

        Task<TReturn> IRequestWaiter.SendRequestAndReceive<TReturn>(
            IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            requestMessage.RequestId = -1;
            return target.Ask<ResponseMessage>(requestMessage, timeout).ContinueWith(t =>
            {
                var response = t.Result;
                if (response.Exception != null)
                {
                    throw response.Exception;
                }
                else
                {
                    var getable = response.ReturnPayload;
                    return (TReturn)getable?.Value;
                }
            });
        }

        public static IRequestWaiter Instance = new DefaultRequestWaiter();
    }
}
