using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    internal class AkkaAskRequestWaiter : IRequestWaiter
    {
        void IRequestWaiter.SendRequest(IRequestTarget target, RequestMessage requestMessage)
        {
            ((AkkaActorTarget)target).Actor.Tell(requestMessage);
        }

        Task IRequestWaiter.SendRequestAndWait(IRequestTarget target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            requestMessage.RequestId = -1;
            return ((AkkaActorTarget)target).Actor.Ask<ResponseMessage>(requestMessage, timeout).ContinueWith(t =>
            {
                if (t.IsCanceled)
                    throw new TaskCanceledException();
                if (t.IsFaulted)
                    throw t.Exception;

                var response = t.Result;
                if (response.Exception != null)
                {
                    throw response.Exception;
                }
            });
        }

        Task<TReturn> IRequestWaiter.SendRequestAndReceive<TReturn>(
            IRequestTarget target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            requestMessage.RequestId = -1;
            return ((AkkaActorTarget)target).Actor.Ask<ResponseMessage>(requestMessage, timeout).ContinueWith(t =>
            {
                if (t.IsCanceled)
                    throw new TaskCanceledException();
                if (t.IsFaulted)
                    throw t.Exception;

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
