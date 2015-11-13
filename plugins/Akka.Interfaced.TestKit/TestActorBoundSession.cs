using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.TestKit
{
    public class TestActorBoundSession : ActorBoundSession
    {
        private readonly IActorRef _self;
        private readonly Func<IActorContext, Tuple<IActorRef, Type>[]> _initialActorFactory;

        private int _lastRequestId;
        private readonly ConcurrentDictionary<int, Action<ResponseMessage>> _requestMap =
            new ConcurrentDictionary<int, Action<ResponseMessage>>();

        private int _lastObserverId;
        private readonly ConcurrentDictionary<int, IInterfacedObserver> _observerMap =
            new ConcurrentDictionary<int, IInterfacedObserver>();

        public TestActorBoundSession(Func<IActorContext, Tuple<IActorRef, Type>[]> initialActorFactory)
        {
            _self = Self;
            _initialActorFactory = initialActorFactory;
        }

        protected override void PreStart()
        {
            base.PreStart();

            var actors = _initialActorFactory(Context);
            if (actors != null)
            {
                foreach (var actor in actors)
                    BindActor(actor.Item1, actor.Item2);
            }
        }

        public IRequestWaiter GetRequestWaiter(int actorId)
        {
            return new RequestWaiter(this, actorId);
        }

        public int IssueObserverId()
        {
            return Interlocked.Increment(ref _lastObserverId);
        }

        public void AddObserver(int observerId, IInterfacedObserver observer)
        {
            _observerMap.TryAdd(observerId, observer);
        }

        public TestObserver AddTestObserver()
        {
            var id = IssueObserverId();
            var observer = new TestObserver(id);
            AddObserver(id, observer);
            return observer;
        }

        public void RemoveObserver(int observerId)
        {
            IInterfacedObserver observer;
            _observerMap.TryRemove(observerId, out observer);
        }

        public IInterfacedObserver GetObserver(int observerId)
        {
            IInterfacedObserver observer;
            return _observerMap.TryGetValue(observerId, out observer)
                       ? observer
                       : null;
        }

        protected override void OnNotificationMessage(NotificationMessage message)
        {
            var observer = GetObserver(message.ObserverId);
            if (observer == null)
            {
                throw new InvalidOperationException(
                    $"Notification didn't find observer. " +
                    $"(ObserverId={message.ObserverId}, Message={message.InvokePayload.GetType().Name})");
            }

            var testObserver = observer as TestObserver;
            if (testObserver != null)
                testObserver.Events.Add(message.InvokePayload);
            else
                message.InvokePayload.Invoke(observer);
        }

        protected override void OnResponseMessage(ResponseMessage message)
        {
            Action<ResponseMessage> handler;
            if (_requestMap.TryRemove(message.RequestId, out handler) == false)
                return;

            handler(message);
        }

        private BoundActor BeginSendRequest(int actorId, RequestMessage requestMessage)
        {
            var a = GetBoundActor(actorId);
            if (a == null)
                throw new InvalidOperationException($"No Actor! (Id={actorId})");

            if (a.InterfaceType != null)
            {
                var msg = (IInterfacedPayload)requestMessage.InvokePayload;
                if (msg == null || msg.GetInterfaceType() != a.InterfaceType)
                    throw new InvalidOperationException("Wrong interface type! " +
                                                        $"(Id={actorId}, Interface={a.InterfaceType})");
            }

            return a;
        }

        private void EndSendRequest(BoundActor a, RequestMessage requestMessage)
        {
            if (a.IsTagOverridable)
            {
                var msg = (ITagOverridable)requestMessage.InvokePayload;
                msg.SetTag(a.TagValue);
            }

            a.Actor.Tell(new RequestMessage
            {
                RequestId = requestMessage.RequestId,
                InvokePayload = requestMessage.InvokePayload
            }, _self);
        }

        private void SendRequest(int actorId, RequestMessage requestMessage)
        {
            var a = BeginSendRequest(actorId, requestMessage);
            EndSendRequest(a, requestMessage);
        }

        private Task SendRequestAndWait(int actorId, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var a = BeginSendRequest(actorId, requestMessage);

            var tcs = new TaskCompletionSource<bool>();
            lock (_requestMap)
            {
                var requestId = Interlocked.Increment(ref _lastRequestId);
                requestMessage.RequestId = requestId;

                var added = _requestMap.TryAdd(requestId, response =>
                {
                    if (response.Exception != null)
                        tcs.SetException(response.Exception);
                    else
                        tcs.SetResult(true);
                });
                if (added == false)
                    throw new InvalidOperationException("Fail to add request.");
            }

            EndSendRequest(a, requestMessage);
            return tcs.Task;
        }

        private Task<TReturn> SendRequestAndReceive<TReturn>(int actorId, RequestMessage requestMessage,
                                                             TimeSpan? timeout)
        {
            var a = BeginSendRequest(actorId, requestMessage);

            var tcs = new TaskCompletionSource<TReturn>();
            lock (_requestMap)
            {
                var requestId = Interlocked.Increment(ref _lastRequestId);
                requestMessage.RequestId = requestId;

                var added = _requestMap.TryAdd(requestId, response =>
                {
                    if (response.Exception != null)
                    {
                        tcs.SetException(response.Exception);
                    }
                    else
                    {
                        var getable = response.ReturnPayload;
                        tcs.SetResult((TReturn)getable?.Value);
                    }
                });
                if (added == false)
                    throw new InvalidOperationException("Fail to add request.");
            }

            EndSendRequest(a, requestMessage);
            return tcs.Task;
        }

        public class RequestWaiter : IRequestWaiter
        {
            private readonly TestActorBoundSession _session;
            private readonly int _actorId;

            public RequestWaiter(TestActorBoundSession session, int actorId)
            {
                _session = session;
                _actorId = actorId;
            }

            void IRequestWaiter.SendRequest(IActorRef target, RequestMessage requestMessage)
            {
                _session.SendRequest(_actorId, requestMessage);
            }

            Task IRequestWaiter.SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
            {
                return _session.SendRequestAndWait(_actorId, requestMessage, timeout);
            }

            Task<TReturn> IRequestWaiter.SendRequestAndReceive<TReturn>(IActorRef target, RequestMessage requestMessage,
                                                                        TimeSpan? timeout)
            {
                return _session.SendRequestAndReceive<TReturn>(_actorId, requestMessage, timeout);
            }
        }
    }
}
