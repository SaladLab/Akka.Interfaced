using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;

namespace Akka.Interfaced.TestKit
{
    public class TestActorBoundSession : ActorBoundSession, IRequestWaiter
    {
        private readonly IActorRef _self;
        private readonly Func<IActorContext, Tuple<IActorRef, ActorBoundSessionMessage.InterfaceType[]>[]> _initialActorFactory;

        private int _lastRequestId;
        private readonly ConcurrentDictionary<int, Action<ResponseMessage>> _requestMap =
            new ConcurrentDictionary<int, Action<ResponseMessage>>();

        private int _lastObserverId;
        private readonly ConcurrentDictionary<int, InterfacedObserver> _observerMap =
            new ConcurrentDictionary<int, InterfacedObserver>();

        private bool _notificationMessagePendingEnabled;
        private readonly ConcurrentQueue<NotificationMessage> _notificationQueue =
            new ConcurrentQueue<NotificationMessage>();

        public bool NotificationMessagePendingEnabled
        {
            get
            {
                return _notificationMessagePendingEnabled;
            }
            set
            {
                if (_notificationMessagePendingEnabled == value)
                    return;

                _notificationMessagePendingEnabled = value;
                if (value == false)
                    ProcessNotificationMessages();
            }
        }

        public TestActorBoundSession(Func<IActorContext, Tuple<IActorRef, ActorBoundSessionMessage.InterfaceType[]>[]> initialActorFactory)
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
                    BindActor(actor.Item1, actor.Item2.Select(t => new BoundType(t)));
            }
        }

        public IActorRef GetBoundActorRef(int id)
        {
            return GetBoundActor(id)?.Actor;
        }

        public IActorRef GetBoundActorRef(InterfacedActorRef actor)
        {
            var boundRef = actor.Actor as BoundActorRef;
            return boundRef != null
                ? GetBoundActorRef(boundRef.Id)
                : null;
        }

        public TRef CreateRef<TRef>(int actorId = 1)
            where TRef : InterfacedActorRef, new()
        {
            var actorRef = new TRef();
            InterfacedActorRefModifier.SetActor(actorRef, new BoundActorRef(actorId));
            InterfacedActorRefModifier.SetRequestWaiter(actorRef, this);
            return actorRef;
        }

        public TObserver CreateObserver<TObserver>(TObserver observer, IList<NotificationMessage> messages = null)
            where TObserver : IInterfacedObserver
        {
            var observerId = IssueObserverId();

            var local = InterfacedObserver.Create(typeof(TObserver));
            local.ObserverId = observerId;
            local.Channel = new TestNotificationChannel { Observer = observer, Messages = messages };
            local.Disposed = () => { RemoveObserver(observerId); };
            AddObserver(observerId, local);

            // duplicate an observer for passing to the actor because
            // proxy observer will be updated via IPayloadObserverUpdatable.
            var proxy = InterfacedObserver.Create(typeof(TObserver));
            proxy.ObserverId = observerId;
            return (TObserver)(object)proxy;
        }

        private int IssueObserverId()
        {
            return Interlocked.Increment(ref _lastObserverId);
        }

        private void AddObserver(int observerId, InterfacedObserver observer)
        {
            _observerMap.TryAdd(observerId, observer);
        }

        private void RemoveObserver(int observerId)
        {
            InterfacedObserver observer;
            _observerMap.TryRemove(observerId, out observer);
        }

        private InterfacedObserver GetObserver(int observerId)
        {
            InterfacedObserver observer;
            return _observerMap.TryGetValue(observerId, out observer)
                       ? observer
                       : null;
        }

        protected override void OnNotificationMessage(NotificationMessage message)
        {
            if (NotificationMessagePendingEnabled)
                _notificationQueue.Enqueue(message);
            else
                InvokeNotificationMessage(message);
        }

        protected override void OnResponseMessage(ResponseMessage message)
        {
            Action<ResponseMessage> handler;
            if (_requestMap.TryRemove(message.RequestId, out handler) == false)
                return;

            var actorRefUpdatable = message.ReturnPayload as IPayloadActorRefUpdatable;
            if (actorRefUpdatable != null)
            {
                actorRefUpdatable.Update(a =>
                    InterfacedActorRefModifier.SetRequestWaiter((InterfacedActorRef)a, this));
            }

            handler(message);
        }

        private Tuple<IActorRef, BoundType> BeginSendRequest(IActorRef actor, RequestMessage requestMessage)
        {
            var actorId = ((BoundActorRef)actor).Id;
            var a = GetBoundActor(actorId);
            if (a == null)
                throw new InvalidOperationException($"No actor. (Id={actorId})");

            var msg = requestMessage.InvokePayload;
            var interfaceType = msg.GetInterfaceType();
            var boundType = a.FindBoundType(interfaceType);
            if (boundType == null)
                throw new InvalidOperationException($"No bound type. (Id={actorId}, Interface={interfaceType})");

            return Tuple.Create(a.Actor, boundType);
        }

        private void EndSendRequest(Tuple<IActorRef, BoundType> target, RequestMessage requestMessage)
        {
            if (target.Item2.IsTagOverridable)
            {
                var msg = (IPayloadTagOverridable)requestMessage.InvokePayload;
                msg.SetTag(target.Item2.TagValue);
            }

            var observerUpdatable = requestMessage.InvokePayload as IPayloadObserverUpdatable;
            if (observerUpdatable != null)
            {
                observerUpdatable.Update(o => ((InterfacedObserver)o).Channel = new ActorNotificationChannel(_self));
            }

            target.Item1.Tell(new RequestMessage
            {
                RequestId = requestMessage.RequestId,
                InvokePayload = requestMessage.InvokePayload
            }, _self);
        }

        void IRequestWaiter.SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            var a = BeginSendRequest(target, requestMessage);
            EndSendRequest(a, requestMessage);
        }

        Task IRequestWaiter.SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var a = BeginSendRequest(target, requestMessage);

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

        Task<TReturn> IRequestWaiter.SendRequestAndReceive<TReturn>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var a = BeginSendRequest(target, requestMessage);

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

        public void ProcessNotificationMessages()
        {
            while (true)
            {
                NotificationMessage message;
                if (_notificationQueue.TryDequeue(out message) == false)
                    break;

                InvokeNotificationMessage(message);
            }
        }

        private void InvokeNotificationMessage(NotificationMessage message)
        {
            var observer = GetObserver(message.ObserverId);
            if (observer == null)
            {
                throw new InvalidOperationException(
                    $"Notification didn't find observer. " +
                    $"(ObserverId={message.ObserverId}, Message={message.InvokePayload.GetType().Name})");
            }

            observer.Channel.Notify(message);
        }
    }
}
