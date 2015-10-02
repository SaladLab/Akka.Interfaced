using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public delegate Task MessageHandler<in T>(T self, object message);

    public class MessageDispatcher<T> where T : class
    {
        public class MessageHandlerInfo
        {
            public bool IsReentrant;
            public bool IsTask;
            public MessageHandler<T> Handler;
        }
        private Dictionary<Type, MessageHandlerInfo> _handlerTable;

        public MessageDispatcher()
        {
            BuildTable();
        }

        private void BuildTable()
        {
            _handlerTable = new Dictionary<Type, MessageHandlerInfo>();

            var type = typeof(T);

            // TODO: BuildHandler ?
            // var handlerBuilder = type.GetMethod("OnBuildHandler", BindingFlags.Static | BindingFlags.NonPublic);

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MessageHandlerAttribute>();
                if (attr == null)
                    continue;

                var messageType = attr.Type ?? method.GetParameters()[0].ParameterType;
                var isTask = (method.ReturnType.Name.StartsWith("Task"));
                var isReentrant = method.CustomAttributes.Any(x => x.AttributeType == typeof(ReentrantAttribute));

                var handler = isTask
                                  ? DelegateBuilderSimpleTask.Build<T>(method)
                                  : DelegateBuilderSimpleFunc.Build<T>(method);

                var info = new MessageHandlerInfo
                {
                    IsReentrant = isReentrant,
                    IsTask = isTask,
                    Handler = (MessageHandler<T>)handler
                };
                _handlerTable.Add(messageType, info);
            }
        }

        public MessageHandlerInfo GetMessageHandler(Type type)
        {
            MessageHandlerInfo info;
            return _handlerTable.TryGetValue(type, out info) ? info : null;
        }
    }
}
