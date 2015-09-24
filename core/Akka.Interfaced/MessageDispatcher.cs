using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public class MessageDispatcher<T> where T : class
    {
        public delegate Task<IValueGetable> MessageHandler(T self, RequestMessage requestMessage);

        public class MessageHandlerInfo
        {
            public Type InterfaceType;
            public bool IsReentrant;
            public MessageHandler Handler;
        }

        private Dictionary<Type, MessageHandlerInfo> _type2InfoMap;

        public MessageDispatcher()
        {
            _type2InfoMap = new Dictionary<Type, MessageHandlerInfo>();

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

                    _type2InfoMap[invokeMessageType] = new MessageHandlerInfo
                    {
                        InterfaceType = ifs,
                        IsReentrant = isReentrant,
                        Handler = handler
                    };
                }
            }
        }

        public MessageHandlerInfo GetHandler(Type type)
        {
            MessageHandlerInfo info;
            return _type2InfoMap.TryGetValue(type, out info) ? info : null;
        }
    }
}
