using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate Task<IValueGetable> RequestMessageHandler<in T>(T self, RequestMessage requestMessage);
    public delegate Task PlainMessageHandler<in T>(T self, object message);

    public class MessageDispatcher<T> where T : class
    {
        public class RequestMessageHandlerInfo
        {
            public Type InterfaceType;
            public bool IsReentrant;
            public RequestMessageHandler<T> Handler;
        }
        private Dictionary<Type, RequestMessageHandlerInfo> _requestMessageTable;

        public class PlainMessageHandlerInfo
        {
            public bool IsReentrant;
            public bool IsTask;
            public PlainMessageHandler<T> Handler;
        }
        private Dictionary<Type, PlainMessageHandlerInfo> _plainMessageTable;

        public MessageDispatcher()
        {
            BuildRequestMessageTable();
            BuildPlainMessageTable();
        }

        private void BuildRequestMessageTable()
        {
            _requestMessageTable = new Dictionary<Type, RequestMessageHandlerInfo>();

            var type = typeof(T);
            var handlerBuilder = type.GetMethod("OnBuildHandler", BindingFlags.Static | BindingFlags.NonPublic);

            // Explicit interface handler

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
                    RequestMessageHandler<T> handler = (self, requestMessage) => requestMessage.Message.Invoke(self);

                    if (handlerBuilder != null)
                    {
                        handler = (RequestMessageHandler<T>)handlerBuilder.Invoke(null, new object[] { handler, targetMethod });
                    }

                    _requestMessageTable[invokeMessageType] = new RequestMessageHandlerInfo
                    {
                        InterfaceType = ifs,
                        IsReentrant = isReentrant,
                        Handler = handler
                    };
                }
            }

            // TODO: Implicit interface handler
        }

        public RequestMessageHandlerInfo GetRequestMessageHandler(Type type)
        {
            RequestMessageHandlerInfo info;
            return _requestMessageTable.TryGetValue(type, out info) ? info : null;
        }

        private void BuildPlainMessageTable()
        {
            _plainMessageTable = new Dictionary<Type, PlainMessageHandlerInfo>();

            var type = typeof(T);
            // var handlerBuilder = type.GetMethod("OnBuildHandler", BindingFlags.Static | BindingFlags.NonPublic);

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MessageDispatchAttribute>();
                if (attr == null)
                    continue;

                var messageType = attr.Type ?? method.GetParameters()[0].ParameterType;
                var isTask = (method.ReturnType.Name.StartsWith("Task"));
                var isReentrant = method.CustomAttributes.Any(x => x.AttributeType == typeof(ReentrantAttribute));

                var handler = isTask
                                  ? DelegateBuilderSimpleTask.Build<T>(method)
                                  : DelegateBuilderSimpleFunc.Build<T>(method);

                var info = new PlainMessageHandlerInfo
                {
                    IsReentrant = isReentrant,
                    IsTask = isTask,
                    Handler = (PlainMessageHandler<T>)handler
                };
                _plainMessageTable.Add(messageType, info);
            }
        }

        public PlainMessageHandlerInfo GetPlainMessageHandler(Type type)
        {
            PlainMessageHandlerInfo info;
            return _plainMessageTable.TryGetValue(type, out info) ? info : null;
        }
    }
}
