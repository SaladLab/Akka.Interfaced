using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public abstract class InterfacedActor<T> : UntypedActor, IWithUnboundedStash, IRequestWaiter
        where T : InterfacedActor<T>
    {
        private static MessageDispatcher<T> Dispatcher = new MessageDispatcher<T>();

        // Stash for stashing incoming messages while atomic handler is running
        public IStash Stash { get; set; }

        // Task context for current atomic task. If no atomic task now, it will be null.
        private MessageHandleContext _currentAtomicContext;

        // Variable to issue unique local observer ID.
        private int _lastIssuedObserverId;

        // ObserverId -> Observer dictionary for bookkeeping registered observers.
        private Dictionary<int, IInterfacedObserver> _observerMap;

        // TODO: Check lock should be required to keep safe?
        private object _requestLock = new object();

        // RequestId -> TCS dictionary for make continuation work when we get response.
        private Dictionary<int, TaskCompletionSource<object>> _requestMap;

        // Variable to issue unique local request ID.
        private int _lastRequestId;

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
                var sender = Sender;

                var handler = Dispatcher.GetRequestMessageHandler(requestMessage.InvokePayload.GetType());
                if (handler == null)
                {
                    sender.Tell(new ResponseMessage
                    {
                        RequestId = requestMessage.RequestId,
                        Exception = new InvalidMessageException("Cannot find handler")
                    });
                    return;
                }

                var context = new MessageHandleContext { Self = Self, Sender = Sender };
                if (handler.IsReentrant == false)
                {
                    BecomeStacked(OnReceiveInAtomicTask);
                    _currentAtomicContext = context;
                }

                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                {
                    handler.Handler((T)this, requestMessage)
                           .ContinueWith(t => OnTaskCompleted(t, Sender, requestMessage, handler),
                                         TaskContinuationOptions.ExecuteSynchronously);
                }
                return;
            }

            var continuationMessage = message as TaskContinuationMessage;
            if (continuationMessage != null)
            {
                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(continuationMessage.Context)))
                {
                    continuationMessage.CallbackAction(continuationMessage.CallbackState);
                }
                return;
            }

            var responseMessage = message as ResponseMessage;
            if (responseMessage != null)
            {
                OnResponseMessage(responseMessage);
                return;
            }

            var noticeMessage = message as NotificationMessage;
            if (noticeMessage != null)
            {
                // Observer 찾기

                if (_observerMap == null)
                    return;

                IInterfacedObserver observer;
                if (_observerMap.TryGetValue(noticeMessage.ObserverId, out observer) == false)
                    return;

                // Observer Call

                noticeMessage.Message.Invoke(observer);

                // - noticeMessage.ObserverId 로 Observer 찾아서
                // - Invokable 실행!
            }

            var taskRunMessage = message as TaskRunMessage;
            if (taskRunMessage != null)
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

            var stopMessage = message as InterfacedPoisonPill;
            if (stopMessage != null)
            {
                var context = new MessageHandleContext { Self = Self, Sender = Sender };
                BecomeStacked(OnReceiveInAtomicTask);
                _currentAtomicContext = context;

                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                {
                    OnPreStop().ContinueWith(t => OnTaskCompleted(false, true),
                                             TaskContinuationOptions.ExecuteSynchronously);
                }
                return;
            }

            var plainMessageHandler = Dispatcher.GetPlainMessageHandler(message.GetType());
            if (plainMessageHandler != null)
            {
                if (plainMessageHandler.IsTask)
                {
                    var context = new MessageHandleContext { Self = Self, Sender = Sender };
                    if (plainMessageHandler.IsReentrant == false)
                    {
                        BecomeStacked(OnReceiveInAtomicTask);
                        _currentAtomicContext = context;
                    }

                    using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                    {
                        plainMessageHandler.Handler((T)this, message)
                                           .ContinueWith(t => OnTaskCompleted(plainMessageHandler.IsReentrant),
                                                         TaskContinuationOptions.ExecuteSynchronously);
                    }
                }
                else
                {
                    plainMessageHandler.Handler((T)this, message);
                }
                return;
            }

            OnReceiveUnhandled(message);
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

        private void OnTaskCompleted(Task<IValueGetable> t, IActorRef sender, RequestMessage requestMessage,
                                     MessageDispatcher<T>.RequestMessageHandlerInfo info)
        {
            try
            {
                if (requestMessage != null && requestMessage.RequestId != 0)
                {
                    if (t.IsFaulted)
                        sender.Tell(new ResponseMessage
                        {
                            RequestId = requestMessage.RequestId,
                            Exception = t.Exception.Flatten().InnerExceptions.FirstOrDefault() ??
                                        t.Exception
                        });
                    else if (t.IsCanceled)
                        sender.Tell(new ResponseMessage
                        {
                            RequestId = requestMessage.RequestId,
                            Exception = new TaskCanceledException()
                        });
                    else
                        sender.Tell(new ResponseMessage { RequestId = requestMessage.RequestId, ReturnPayload = t.Result });
                }

                if (info.IsReentrant == false)
                {
                    _currentAtomicContext = null;
                    UnbecomeStacked();
                    Stash.UnstashAll();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!!! ERROR in OnTaskCompleted !!!");
                Console.WriteLine(e);
            }
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

        Task<object> IRequestWaiter.SendRequestAndReceive(IActorRef target, RequestMessage requestMessage,
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

            requestMessage.RequestId = requestId;
            target.Tell(requestMessage, Self);
            return tcs.Task;
        }

        private void OnResponseMessage(ResponseMessage response)
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
                tcs.SetResult(response.ReturnPayload != null ? response.ReturnPayload.Value : null);
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

        protected int IssueObserverId()
        {
            return ++_lastIssuedObserverId;
        }

        protected void AddObserver(int observerId, IInterfacedObserver observer)
        {
            if (_observerMap == null)
                _observerMap = new Dictionary<int, IInterfacedObserver>();

            _observerMap.Add(observerId, observer);
        }

        protected IInterfacedObserver GetObserver(int observerId)
        {
            if (_observerMap == null)
                return null;

            IInterfacedObserver observer;
            return _observerMap.TryGetValue(observerId, out observer) ? observer : null;
        }

        protected bool RemoveObserver(int observerId)
        {
            if (_observerMap == null)
                return false;

            return _observerMap.Remove(observerId);
        }
    }
}
