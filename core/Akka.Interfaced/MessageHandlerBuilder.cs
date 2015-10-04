using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced
{
    public static class MessageHandlerBuilder<T> where T : class
    {
        public static Dictionary<Type, MessageHandlerItem<T>> BuildTable()
        {
            var table = new Dictionary<Type, MessageHandlerItem<T>>();

            var type = typeof(T);
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MessageHandlerAttribute>();
                if (attr == null)
                    continue;

                var messageType = attr.Type ?? method.GetParameters()[0].ParameterType;
                var isAsyncMethod = (method.ReturnType.Name.StartsWith("Task"));
                if (isAsyncMethod)
                {
                    var item = new MessageHandlerItem<T>
                    {
                        IsReentrant = IsReentrantMethod(method),
                        AsyncHandler = MessageHandlerAsyncBuilder.Build<T>(method)
                    };
                    table.Add(messageType, item);
                }
                else
                {
                    var item = new MessageHandlerItem<T>
                    {
                        Handler = MessageHandlerFuncBuilder.Build<T>(method)
                    };
                    table.Add(messageType, item);
                }
            }

            return table;
        }

        private static bool IsReentrantMethod(MethodInfo method)
        {
            return method.CustomAttributes.Any(x => x.AttributeType == typeof(ReentrantAttribute));
        }
    }
}
