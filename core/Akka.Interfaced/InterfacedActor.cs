using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedActor<T> : UntypedActor, IWithUnboundedStash, IRequestWaiter, IFilterPerInstanceProvider
        where T : InterfacedActor<T>
    {
        private readonly static RequestDispatcher<T> RequestDispatcher;
        private readonly static List<Func<object, IFilter>> PerInstanceFilterCreators;
        private readonly static MessageDispatcher<T> MessageDispatcher;

        static InterfacedActor()
        {
            var requestHandlerBuilder = new RequestHandlerBuilder<T>();
            requestHandlerBuilder.Build();
            RequestDispatcher = new RequestDispatcher<T>(requestHandlerBuilder.HandlerTable);
            PerInstanceFilterCreators = requestHandlerBuilder.PerInstanceFilterCreators;

            MessageDispatcher = new MessageDispatcher<T>();
        }

        // Stash for stashing incoming messages while atomic handler is running
        public IStash Stash { get; set; }

        // Task context for current atomic task. If no atomic task now, it will be null.
        private MessageHandleContext _currentAtomicContext;

        private InterfacedActorRequestWaiter _requestWaiter;
        private InterfacedActorObserverMap _observerMap;
        private InterfacedActorPerInstanceFilterList _perInstanceFilterList;

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

            var context = new MessageHandleContext { Self = Self, Sender = Sender };
            BecomeStacked(OnReceiveInAtomicTask);
            _currentAtomicContext = context;

            using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
            {
                OnPreStart().ContinueWith(
                    t => OnTaskCompleted(false),
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
            var sender = Sender;

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

                var context = new MessageHandleContext { Self = Self, Sender = Sender };
                if (handlerItem.IsReentrant == false)
                {
                    BecomeStacked(OnReceiveInAtomicTask);
                    _currentAtomicContext = context;
                }

                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                {
                    var requestId = request.RequestId;
                    var IsReentrant = handlerItem.IsReentrant;
                    handlerItem.AsyncHandler((T)this, request, response =>
                    {
                        if (requestId != 0)
                            sender.Tell(response);
                        OnTaskCompleted(IsReentrant);
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
            if (_requestWaiter != null)
                _requestWaiter.OnResponseMessage(response);
        }

        private void OnNotificationMessage(NotificationMessage notification)
        {
            if (_observerMap != null)
                _observerMap.Notify(notification);
        }

        private void OnTaskRunMessage(TaskRunMessage taskRunMessage)
        {
            var context = new MessageHandleContext { Self = Self, Sender = Sender };
            if (taskRunMessage.IsReentrant == false)
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
            return;
        }

        private void OnInterfacedPoisonPill()
        {
            var context = new MessageHandleContext { Self = Self, Sender = Sender };
            BecomeStacked(OnReceiveInAtomicTask);
            _currentAtomicContext = context;

            using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
            {
                OnPreStop().ContinueWith(t => OnTaskCompleted(false, true),
                                         TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        private void HandleMessageByHandler(object message, MessageHandlerItem<T> handlerItem)
        {
            if (handlerItem.AsyncHandler != null)
            {
                var context = new MessageHandleContext { Self = Self, Sender = Sender };
                if (handlerItem.IsReentrant == false)
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

        private void OnTaskCompleted(bool isReentrant, bool stopOnCompleted = false)
        {
            if (isReentrant == false)
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

        Task<object> IRequestWaiter.SendRequestAndReceive(IActorRef target, RequestMessage request,
                                                          TimeSpan? timeout)
        {
            if (_requestWaiter == null)
                _requestWaiter = new InterfacedActorRequestWaiter();

            return _requestWaiter.SendRequestAndReceive(target, request, Self, timeout);
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
            if (_observerMap == null)
                _observerMap = new InterfacedActorObserverMap();
            return _observerMap;
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
            return _observerMap != null ? _observerMap.Get(observerId) : null;
        }

        protected bool RemoveObserver(int observerId)
        {
            return _observerMap != null ? _observerMap.Remove(observerId) : false;
        }

        // PerInstance Filter related

        IFilter IFilterPerInstanceProvider.GetFilter(int index)
        {
            return _perInstanceFilterList.Get(index);
        }
    }
}
