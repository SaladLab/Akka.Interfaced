using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    internal class InterfacedActorRequestWaiter
    {
        private int _lastRequestId;

        private struct ResponseWaitingItem
        {
            public Action<object, ResponseMessage> ResponseHandler;
            public Action<object> CancelHandler;
            public object TaskCompletionSource;
        }

        private readonly ConcurrentDictionary<int, ResponseWaitingItem> _responseWaitingItems =
            new ConcurrentDictionary<int, ResponseWaitingItem>();

        public Task SendRequestAndWait(
            IActorRef target, RequestMessage request, IActorRef sender, TimeSpan? timeout)
        {
            return SendRequestAndReceive<object>(target, request, sender, timeout);
        }

        public Task<TReturn> SendRequestAndReceive<TReturn>(
            IActorRef target, RequestMessage request, IActorRef sender, TimeSpan? timeout)
        {
            // Issue requestId and register it in table

            var tcs = new TaskCompletionSource<TReturn>();
            int requestId;

            while (true)
            {
                requestId = ++_lastRequestId;
                if (requestId < 0)
                    requestId = _lastRequestId = 1;

                var added = _responseWaitingItems.TryAdd(requestId, new ResponseWaitingItem
                {
                    ResponseHandler = (taskCompletionSource, response) =>
                    {
                        var completionSource = ((TaskCompletionSource<TReturn>)taskCompletionSource);
                        if (response.Exception != null)
                            completionSource.SetException(response.Exception);
                        else
                            completionSource.SetResult((TReturn)response.ReturnPayload?.Value);
                    },
                    CancelHandler = (taskCompletionSource) =>
                    {
                        var completionSource = ((TaskCompletionSource<TReturn>)taskCompletionSource);
                        completionSource.SetCanceled();
                    },
                    TaskCompletionSource = tcs
                });

                if (added)
                    break;
            }

            // Set timeout

            if (timeout != null && timeout.Value != Timeout.InfiniteTimeSpan && timeout.Value > default(TimeSpan))
            {
                var cancellationSource = new CancellationTokenSource();
                cancellationSource.Token.Register(() =>
                {
                    ResponseWaitingItem waitingItem;
                    if (_responseWaitingItems.TryRemove(requestId, out waitingItem))
                    {
                        waitingItem.CancelHandler(waitingItem.TaskCompletionSource);
                    }
                });
                cancellationSource.CancelAfter(timeout.Value);
            }

            // Fire request

            request.RequestId = requestId;
            target.Tell(request, sender);
            return tcs.Task;
        }

        public void OnResponseMessage(ResponseMessage response, MessageHandleContext currentAtomicContext)
        {
            ResponseWaitingItem waitingItem;
            if (_responseWaitingItems.TryRemove(response.RequestId, out waitingItem) == false)
                return;

            // Because OnResponseMessage is always called in a message loop of actor,
            // it's safe to run post callback synchronously if possible.
            // By this optimization one message hop can be removed.
            ActorSynchronizationContext.EnableSynchronousPost(currentAtomicContext);

            waitingItem.ResponseHandler(waitingItem.TaskCompletionSource, response);
        }
    }
}
