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
        public delegate Task<IValueGetable> MessageHandler(T self, RequestMessage requestMessage);

        private class MessageHandlerInfo
        {
            public Type InterfaceType;
            public bool IsReentrant;
            public MessageHandler Handler;
        }

        private static Dictionary<Type, MessageHandlerInfo> Type2InfoMap;

        static InterfacedActor()
        {
            Type2InfoMap = new Dictionary<Type, MessageHandlerInfo>();

            var type = typeof(T);
            var handlerBuilder = type.GetMethod("OnBuildHandler", BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var ifs in type.GetInterfaces())
            {
                if (ifs.GetInterfaces().All(t => t != typeof(IInterfacedActor)))
                    continue;

                var interfaceMap = type.GetInterfaceMap(ifs);

                var messageTableType =
                    interfaceMap.InterfaceType.Assembly.GetTypes()
                                .Where(t =>
                                {
                                    var attr = t.GetCustomAttribute<MessageTableForInterfacedActorAttribute>();
                                    return (attr != null && attr.Type == ifs);
                                })
                                .FirstOrDefault();

                if (messageTableType == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot find message table class for {0}", ifs.FullName));
                }

                var queryMethodInfo = messageTableType.GetMethod("GetMessageTypes");
                if (queryMethodInfo == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot find {0}.GetMessageTypes method", messageTableType.FullName));
                }

                var messageTable = (Type[,])queryMethodInfo.Invoke(null, new object[] { });
                if (messageTable == null || messageTable.GetLength(0) != interfaceMap.InterfaceMethods.Length)
                {
                    throw new InvalidOperationException(
                        string.Format("Mismatched messageTable from {0}", messageTableType.FullName));
                }

                for (var i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
                {
                    var interfaceMethod = interfaceMap.InterfaceMethods[i];
                    var targetMethod = interfaceMap.TargetMethods[i];
                    var invokeMessageType = messageTable[i, 0];

                    var isReentrant = targetMethod.CustomAttributes
                                                  .Any(x => x.AttributeType == typeof(ReentrantAttribute));
                    MessageHandler handler = (self, requestMessage) => requestMessage.Message.Invoke(self);

                    if (handlerBuilder != null)
                    {
                        handler = (MessageHandler)handlerBuilder.Invoke(null, new object[] { handler, targetMethod });
                    }

                    Type2InfoMap[invokeMessageType] = new MessageHandlerInfo
                    {
                        InterfaceType = ifs,
                        IsReentrant = isReentrant,
                        Handler = handler
                    };
                }
            }

            Console.WriteLine("# Build<{0}> has {1} items", type.Name, Type2InfoMap.Count);
        }

        // Atomic Task 를 처리중일 때 들어오는 메시지를 지연 처리하기 위한 Stash
        public IStash Stash { get; set; }

        // 현재 진행중인 Atomic Task 의 컨텍스트. 진행중인 Atomic Task 가 없으면 Null.
        private MessageHandleContext _currentAtomicContext;

        // ObserverId -> Observer dictionary for bookkeeping registered observers
        private Dictionary<int, IInterfacedObserver> _observerMap;

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

                MessageHandlerInfo info;
                if (Type2InfoMap.TryGetValue(requestMessage.Message.GetType(), out info) == false)
                {
                    sender.Tell(new ReplyMessage
                    {
                        RequestId = requestMessage.RequestId,
                        Exception = new InvalidMessageException("Cannot find handler")
                    });
                    return;
                }

                var context = new MessageHandleContext { Self = Self, Sender = Sender };
                if (info.IsReentrant == false)
                {
                    BecomeStacked(OnReceiveInAtomicTask);
                    _currentAtomicContext = context;
                }

                using (new SynchronizationContextSwitcher(new ActorSynchronizationContext(context)))
                {
                    info.Handler((T)this, requestMessage)
                        .ContinueWith(t => OnTaskCompleted(t, Sender, requestMessage, info),
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

            var replyMessage = message as ReplyMessage;
            if (replyMessage != null)
            {
                OnReplyMessage(replyMessage);
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

            var replyMessage = message as ReplyMessage;
            if (replyMessage != null)
            {
                OnReplyMessage(replyMessage);
                return;
            }

            Stash.Stash();
        }

        private void OnTaskCompleted(Task<IValueGetable> t, IActorRef sender, RequestMessage requestMessage,
                                     MessageHandlerInfo info)
        {
            try
            {
                if (requestMessage != null && requestMessage.RequestId != 0)
                {
                    if (t.IsFaulted)
                        sender.Tell(new ReplyMessage
                        {
                            RequestId = requestMessage.RequestId,
                            Exception =
                                t.Exception.Flatten().InnerExceptions.FirstOrDefault() ??
                                t.Exception
                        });
                    else if (t.IsCanceled)
                        sender.Tell(new ReplyMessage
                        {
                            RequestId = requestMessage.RequestId,
                            Exception = new TaskCanceledException()
                        });
                    else
                        sender.Tell(new ReplyMessage { RequestId = requestMessage.RequestId, Result = t.Result });
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

        private object _requestLock = new object();
        private Dictionary<int, TaskCompletionSource<object>> _requestMap;
        private int _lastRequestId;

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

        private void OnReplyMessage(ReplyMessage replyMessage)
        {
            TaskCompletionSource<object> tcs;
            lock (_requestLock)
            {
                if (_requestMap == null || _requestMap.TryGetValue(replyMessage.RequestId, out tcs) == false)
                    return;
            }

            if (replyMessage.Exception != null)
                tcs.SetException(replyMessage.Exception);
            else
                tcs.SetResult(replyMessage.Result != null ? replyMessage.Result.Value : null);
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
    }
}
