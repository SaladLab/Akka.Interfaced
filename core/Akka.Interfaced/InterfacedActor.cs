using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedActor<T> : UntypedActor, IWithUnboundedStash, IRequestWaiter, IFilterPerInstanceProvider
        where T : InterfacedActor<T>
    {
        #region Static Variables

        private readonly static RequestDispatcher<T> RequestDispatcher;
        private readonly static NotificationDispatcher<T> NotificationDispatcher;
        private readonly static MessageDispatcher<T> MessageDispatcher;
        private readonly static List<Func<object, IFilter>> PerInstanceFilterCreators;

        static InterfacedActor()
        {
            var filterHandlerBuilder = new FilterHandlerBuilder(typeof(T));

            var requestHandlerBuilder = new RequestHandlerBuilder<T>();
            RequestDispatcher = new RequestDispatcher<T>(requestHandlerBuilder.Build(filterHandlerBuilder));

            var notificationHandlerBuilder = new NotificationHandlerBuilder<T>();
            NotificationDispatcher = new NotificationDispatcher<T>(notificationHandlerBuilder.Build(filterHandlerBuilder));

            var messageHandlerBuilder = new MessageHandlerBuilder<T>();
            MessageDispatcher = new MessageDispatcher<T>(messageHandlerBuilder.Build(filterHandlerBuilder));

            PerInstanceFilterCreators = filterHandlerBuilder.PerInstanceFilterCreators;
        }

        #endregion

        #region Member Variables

        public IStash Stash { get; set; }
        private int _activeReentrantCount;
        private MessageHandleContext _currentAtomicContext;
        private InterfacedActorRequestWaiter _requestWaiter;
        private InterfacedActorObserverMap _observerMap;
        private InterfacedActorPerInstanceFilterList _perInstanceFilterList;

        #endregion

        protected new IActorRef Sender
        {
            get
            {
                var context = ActorSynchronizationContext.GetCurrentContext();
                return context != null ? context.Sender : base.Sender;
            }
        }

        // Atomic async OnPreStart event (it will be called after PreStart)
        protected virtual Task OnPreStart()
        {
            return Task.FromResult(true);
        }

        // Atomic async OnPreStop event (it will be called when receives StopMessage)
        // After finishing this call, actor will be stopped.
        protected virtual Task OnPreStop()
        {
            return Task.FromResult(true);
        }

        protected override void PreStart()
        {
            if (PerInstanceFilterCreators.Count > 0)
                _perInstanceFilterList = new InterfacedActorPerInstanceFilterList(this, PerInstanceFilterCreators);

            InvokeOnPreStart();
        }

        private void InvokeOnPreStart()
        {
            var context = new MessageHandleContext { Self = Self, Sender = Sender };
            BecomeStacked(OnReceiveInAtomicTask);
            _currentAtomicContext = context;

            using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
            {
                OnPreStart().ContinueWith(t => OnTaskCompleted(false),
                                          TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void InvokeOnPreStop()
        {
            var context = new MessageHandleContext { Self = Self };
            BecomeStacked(OnReceiveInAtomicTask);
            _currentAtomicContext = context;

            using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
            {
                OnPreStop().ContinueWith(t => OnTaskCompleted(false, true),
                                         TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        protected override void OnReceive(object message)
        {
            var requestMessage = message as RequestMessage;
            if (requestMessage != null)
            {
                OnRequestMessage(requestMessage);
                return;
            }

            var continuationMessage = message as TaskContinuationMessage;
            if (continuationMessage != null)
            {
                OnTaskContinuationMessage(continuationMessage);
                return;
            }

            var responseMessage = message as ResponseMessage;
            if (responseMessage != null)
            {
                OnResponseMessage(responseMessage);
                return;
            }

            var notificationMessage = message as NotificationMessage;
            if (notificationMessage != null)
            {
                OnNotificationMessage(notificationMessage);
                return;
            }

            var taskRunMessage = message as TaskRunMessage;
            if (taskRunMessage != null)
            {
                OnTaskRunMessage(taskRunMessage);
                return;
            }

            var poisonPill = message as InterfacedPoisonPill;
            if (poisonPill != null)
            {
                OnInterfacedPoisonPill();
                return;
            }

            var messageHandler = MessageDispatcher.GetHandler(message.GetType());
            if (messageHandler != null)
            {
                HandleMessageByHandler(message, messageHandler);
                return;
            }

            OnReceiveUnhandled(message);
        }

        private void OnRequestMessage(RequestMessage request)
        {
            var sender = base.Sender;

            if (request.InvokePayload == null)
            {
                sender.Tell(new ResponseMessage
                {
                    RequestId = request.RequestId,
                    Exception = new InvalidMessageException("Empty payload")
                });
                return;
            }

            var handlerItem = RequestDispatcher.GetHandler(request.InvokePayload.GetType());
            if (handlerItem == null)
            {
                sender.Tell(new ResponseMessage
                {
                    RequestId = request.RequestId,
                    Exception = new InvalidMessageException("Cannot find handler")
                });
                return;
            }

            if (handlerItem.Handler != null)
            {
                // sync handle

                var response = handlerItem.Handler((T)this, request, null);
                if (request.RequestId != 0)
                    sender.Tell(response);
            }
            else
            {
                // async handle

                var context = new MessageHandleContext { Self = Self, Sender = base.Sender };
                if (handlerItem.IsReentrant)
                {
                    _activeReentrantCount += 1;
                }
                else
                {
                    BecomeStacked(OnReceiveInAtomicTask);
                    _currentAtomicContext = context;
                }

                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                {
                    var requestId = request.RequestId;
                    var isReentrant = handlerItem.IsReentrant;
                    handlerItem.AsyncHandler((T)this, request, response =>
                    {
                        if (requestId != 0)
                            sender.Tell(response);
                        OnTaskCompleted(isReentrant);
                    });
                }
            }
        }

        private static void OnTaskContinuationMessage(TaskContinuationMessage continuation)
        {
            using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(continuation.Context)))
            {
                continuation.CallbackAction(continuation.CallbackState);
            }
        }

        private void OnResponseMessage(ResponseMessage response)
        {
            _requestWaiter?.OnResponseMessage(response, _currentAtomicContext);
        }

        private void OnNotificationMessage(NotificationMessage notification)
        {
            if (notification.ObserverId == 0)
            {
                var handlerItem = NotificationDispatcher.GetHandler(notification.InvokePayload.GetType());
                if (handlerItem == null)
                {
                    // TODO: log no handler.
                    return;
                }

                if (handlerItem.Handler != null)
                {
                    // sync handle

                    handlerItem.Handler((T)this, notification);
                }
                else
                {
                    // async handle

                    var context = new MessageHandleContext { Self = Self, Sender = base.Sender };
                    if (handlerItem.IsReentrant)
                    {
                        _activeReentrantCount += 1;
                    }
                    else
                    {
                        BecomeStacked(OnReceiveInAtomicTask);
                        _currentAtomicContext = context;
                    }

                    using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                    {
                        handlerItem.AsyncHandler((T)this, notification)
                                   .ContinueWith(t => OnTaskCompleted(handlerItem.IsReentrant),
                                                 TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
            }
            else
            {
                _observerMap?.Notify(notification);
            }
        }

        private void OnTaskRunMessage(TaskRunMessage taskRunMessage)
        {
            var context = new MessageHandleContext { Self = Self, Sender = base.Sender };
            if (taskRunMessage.IsReentrant)
            {
                _activeReentrantCount += 1;
            }
            else
            {
                BecomeStacked(OnReceiveInAtomicTask);
                _currentAtomicContext = context;
            }

            using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
            {
                taskRunMessage.Function()
                              .ContinueWith(t => OnTaskCompleted(taskRunMessage.IsReentrant),
                                            TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void OnInterfacedPoisonPill()
        {
            if (_activeReentrantCount > 0)
            {
                BecomeStacked(OnReceiveInWaitingForReentrantsFinished);
            }
            else
            {
                InvokeOnPreStop();
            }
        }

        private void HandleMessageByHandler(object message, MessageHandlerItem<T> handlerItem)
        {
            if (handlerItem.AsyncHandler != null)
            {
                var context = new MessageHandleContext { Self = Self, Sender = base.Sender };
                if (handlerItem.IsReentrant)
                {
                    _activeReentrantCount += 1;
                }
                else
                {
                    BecomeStacked(OnReceiveInAtomicTask);
                    _currentAtomicContext = context;
                }

                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                {
                    handlerItem.AsyncHandler((T)this, message)
                               .ContinueWith(t => OnTaskCompleted(handlerItem.IsReentrant),
                                             TaskContinuationOptions.ExecuteSynchronously);
                }
            }
            else
            {
                handlerItem.Handler((T)this, message);
            }
        }

        private void OnReceiveInAtomicTask(object message)
        {
            var continuationMessage = message as TaskContinuationMessage;
            if (continuationMessage != null && continuationMessage.Context == _currentAtomicContext)
            {
                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(continuationMessage.Context)))
                {
                    continuationMessage.CallbackAction(continuationMessage.CallbackState);
                }
                return;
            }

            var response = message as ResponseMessage;
            if (response != null)
            {
                OnResponseMessage(response);
                return;
            }

            Stash.Stash();
        }

        private void OnReceiveInWaitingForReentrantsFinished(object message)
        {
            var continuationMessage = message as TaskContinuationMessage;
            if (continuationMessage != null)
            {
                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(continuationMessage.Context)))
                {
                    continuationMessage.CallbackAction(continuationMessage.CallbackState);
                }

                if (_activeReentrantCount == 0)
                {
                    UnbecomeStacked();
                    InvokeOnPreStop();
                }
                return;
            }

            var response = message as ResponseMessage;
            if (response != null)
            {
                OnResponseMessage(response);
                return;
            }

            Stash.Stash();
        }

        private void OnTaskCompleted(bool isReentrant, bool stopOnCompleted = false)
        {
            if (isReentrant)
            {
                _activeReentrantCount -= 1;
            }
            else
            {
                _currentAtomicContext = null;
                UnbecomeStacked();
                Stash.UnstashAll();
            }

            if (stopOnCompleted)
            {
                Context.Stop(Self);
            }
        }

        // from IRequestWaiter

        void IRequestWaiter.SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            target.Tell(requestMessage);
        }

        Task IRequestWaiter.SendRequestAndWait(
            IActorRef target, RequestMessage request, TimeSpan? timeout)
        {
            if (_requestWaiter == null)
                _requestWaiter = new InterfacedActorRequestWaiter();

            return _requestWaiter.SendRequestAndWait(target, request, Self, timeout);
        }

        Task<TReturn> IRequestWaiter.SendRequestAndReceive<TReturn>(
            IActorRef target, RequestMessage request, TimeSpan? timeout)
        {
            if (_requestWaiter == null)
                _requestWaiter = new InterfacedActorRequestWaiter();

            return _requestWaiter.SendRequestAndReceive<TReturn>(target, request, Self, timeout);
        }

        // async support

        protected new void RunTask(Action action)
        {
            RunTask(() =>
            {
                action();
                return Task.FromResult(0);
            });
        }

        protected new void RunTask(Func<Task> function)
        {
            RunTask(function, false);
        }

        protected void RunTask(Func<Task> function, bool isReentrant)
        {
            Self.Tell(new TaskRunMessage { Function = function, IsReentrant = isReentrant });
        }

        // other messages

        protected virtual void OnReceiveUnhandled(object message)
        {
            Unhandled(message);
        }

        // observer support

        private InterfacedActorObserverMap EnsureObserverMap()
        {
            return _observerMap ?? (_observerMap = new InterfacedActorObserverMap());
        }

        protected int IssueObserverId()
        {
            return EnsureObserverMap().IssueId();
        }

        protected void AddObserver(int observerId, IInterfacedObserver observer)
        {
            EnsureObserverMap().Add(observerId, observer);
        }

        protected IInterfacedObserver GetObserver(int observerId)
        {
            return _observerMap?.Get(observerId);
        }

        protected bool RemoveObserver(int observerId)
        {
            return _observerMap?.Remove(observerId) ?? false;
        }

        // PerInstance Filter related

        IFilter IFilterPerInstanceProvider.GetFilter(int index)
        {
            return _perInstanceFilterList.Get(index);
        }
    }
}
