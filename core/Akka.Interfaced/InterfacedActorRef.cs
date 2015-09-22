using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedActorRef
    {
        public IActorRef Actor { get; protected set; }
        public IRequestWaiter RequestWaiter { get; protected set; }
        public TimeSpan? Timeout { get; protected set; }

        public InterfacedActorRef(IActorRef actor)
        {
            Actor = actor;
            RequestWaiter = DefaultRequestWaiter.Instance;
        }

        public InterfacedActorRef(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout = null)
        {
            Actor = actor;
            RequestWaiter = requestWaiter;
            Timeout = timeout;
        }

        // Request & Reply

        private static readonly Task CompletedTask = Task.FromResult(true);

        protected Task SendRequestAndWait(RequestMessage requestMessage)
        {
            if (RequestWaiter != null)
            {
                return RequestWaiter.SendRequestAndReceive(Actor, requestMessage, Timeout);
            }
            else
            {
                Actor.Tell(requestMessage);
                return CompletedTask;
            }
        }

        protected Task<T> SendRequestAndReceive<T>(RequestMessage requestMessage)
        {
            if (RequestWaiter != null)
            {
                var task = RequestWaiter.SendRequestAndReceive(Actor, requestMessage, Timeout);
                return task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        throw t.Exception.Flatten().InnerExceptions.FirstOrDefault() ?? t.Exception;
                    else if (t.IsCanceled)
                        throw new TaskCanceledException();
                    else
                        return (T) t.Result;
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            else
            {
                Actor.Tell(requestMessage);
                return Task.FromResult(default(T));
            }
        }
    }

    internal class DefaultRequestWaiter : IRequestWaiter
    {
        Task<object> IRequestWaiter.SendRequestAndReceive(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            requestMessage.RequestId = -1;
            return target.Ask<ReplyMessage>(requestMessage, timeout).ContinueWith(t =>
            {
                var replyMessage = t.Result;
                if (replyMessage.Exception != null)
                {
                    throw replyMessage.Exception;
                }
                else
                {
                    var getable = replyMessage.Result;
                    if (getable != null)
                        return getable.Value;
                    else
                        return null;
                }
            });
        }

        public static IRequestWaiter Instance = new DefaultRequestWaiter();
    }
}
