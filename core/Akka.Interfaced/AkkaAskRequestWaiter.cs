using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    internal class AkkaAskRequestWaiter : IRequestWaiter
    {
        void IRequestWaiter.SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            target.Tell(requestMessage);
        }

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

        public static IRequestWaiter Instance = new AkkaAskRequestWaiter();
    }
}
