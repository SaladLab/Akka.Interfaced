using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    internal class InterfacedActorRequestWaiter
    {
        private object _requestLock = new object();
        private int _lastRequestId;
        private Dictionary<int, TaskCompletionSource<object>> _requestMap =
            new Dictionary<int, TaskCompletionSource<object>>();

        public Task<object> SendRequestAndReceive(IActorRef target, RequestMessage request, IActorRef sender,
                                                  TimeSpan? timeout)
        {
            // Issue requestId and register it in table

            int requestId;
            TaskCompletionSource<object> tcs;

            lock (_requestLock)
            {
                if (_requestMap == null)
                    _requestMap = new Dictionary<int, TaskCompletionSource<object>>();

                requestId = ++_lastRequestId;
                if (requestId < 0)
                    requestId = 1;

                tcs = new TaskCompletionSource<object>();
                _requestMap[requestId] = tcs;
            }

            // Set timeout

            if (timeout != null && timeout.Value != Timeout.InfiniteTimeSpan && timeout.Value > default(TimeSpan))
            {
                var cancellationSource = new CancellationTokenSource();
                cancellationSource.Token.Register(() =>
                {
                    lock (_requestLock)
                    {
                        _requestMap.Remove(requestId);
                    }
                    tcs.TrySetCanceled();
                });
                cancellationSource.CancelAfter(timeout.Value);
            }

            // Fire request

            request.RequestId = requestId;
            target.Tell(request, sender);
            return tcs.Task;
        }

        public void OnResponseMessage(ResponseMessage response)
        {
            TaskCompletionSource<object> tcs;
            lock (_requestLock)
            {
                if (_requestMap == null || _requestMap.TryGetValue(response.RequestId, out tcs) == false)
                    return;
            }

            if (response.Exception != null)
                tcs.SetException(response.Exception);
            else
                tcs.SetResult(response.ReturnPayload?.Value);
        }
    }
}
