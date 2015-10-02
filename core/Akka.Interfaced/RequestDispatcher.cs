using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Akka.Interfaced
{
    public delegate Task<IValueGetable> RequestHandler<in T>(T self, RequestMessage requestMessage);

    public class RequestDispatcher<T> where T : class
    {
        public class RequestHandlerInfo
        {
            public Type InterfaceType;
            public bool IsReentrant;
            public RequestHandler<T> Handler;
        }
        private Dictionary<Type, RequestHandlerInfo> _handlerTable;

        public RequestDispatcher()
        {
            BuildTable();
        }

        private void BuildTable()
        {
            _handlerTable = new Dictionary<Type, RequestHandlerInfo>();

            var type = typeof(T);
            var handlerBuilder = type.GetMethod("OnBuildHandler", BindingFlags.Static | BindingFlags.NonPublic);

            // Regular interface handler

            foreach (var ifs in type.GetInterfaces())
            {
                if (ifs.GetInterfaces().All(t => t != typeof(IInterfacedActor)))
                    continue;

                var interfaceMap = type.GetInterfaceMap(ifs);
                var messageTable = GetInterfacePayloadTypeTable(ifs);

                for (var i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
                {
                    var targetMethod = interfaceMap.TargetMethods[i];
                    var invokeMessageType = messageTable[i, 0];

                    var isReentrant = targetMethod.CustomAttributes
                                                  .Any(x => x.AttributeType == typeof(ReentrantAttribute));
                    RequestHandler<T> handler = (self, requestMessage) => requestMessage.InvokePayload.Invoke(self);

                    if (handlerBuilder != null)
                    {
                        handler = (RequestHandler<T>)handlerBuilder.Invoke(null, new object[] { handler, targetMethod });
                    }

                    _handlerTable[invokeMessageType] = new RequestHandlerInfo
                    {
                        InterfaceType = ifs,
                        IsReentrant = isReentrant,
                        Handler = handler
                    };
                }
            }

            // Extended interface handler

            var targetMethods =
                type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(m => m.GetCustomAttribute<ExtendedHandlerAttribute>() != null)
                    .Select(m => Tuple.Create(m, m.GetCustomAttribute<ExtendedHandlerAttribute>()))
                    .ToList();
            var extendedInterfaces =
                type.GetInterfaces()
                    .Where(t => t.FullName.StartsWith("Akka.Interfaced.IExtendedInterface"))
                    .SelectMany(t => t.GenericTypeArguments);
            foreach (var ifs in extendedInterfaces)
            {
                var messageTable = GetInterfacePayloadTypeTable(ifs);
                var interfaceMethods = ifs.GetMethods();

                for (var i = 0; i < interfaceMethods.Length; i++)
                {
                    var interfaceMethod = interfaceMethods[i];
                    var invokeMessageType = messageTable[i, 0];
                    var name = interfaceMethod.Name;
                    var parameters = interfaceMethod.GetParameters();

                    // find method which can handle this invoke message

                    MethodInfo targetMethod = null;
                    foreach (var method in targetMethods)
                    {
                        if (method.Item1.Name == name && (method.Item2.Type == null || method.Item2.Type == ifs) &&
                            AreParameterTypesEqual(method.Item1.GetParameters(), parameters))
                        {
                            if (targetMethod != null)
                            {
                                throw new InvalidOperationException(
                                    $"Ambiguous handlers for {ifs.FullName}.{interfaceMethod.Name} method.\n" +
                                    $" {targetMethod.Name}\n {method.Item1.Name}\n");
                            }
                            targetMethod = method.Item1;
                        }
                    }
                    if (targetMethod == null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot find handler for {ifs.FullName}.{interfaceMethod.Name}");
                    }
                    targetMethods.RemoveAll(x => x.Item1 == targetMethod);

                    // create handler

                    var isReentrant = targetMethod.CustomAttributes
                                                     .Any(x => x.AttributeType == typeof(ReentrantAttribute));

                    var isTask = targetMethod.ReturnType.Name.StartsWith("Task");
                    var invokeDelegate =
                        isTask
                            ? DelegateBuilderHandlerExtendedTask.Build<T>(
                                messageTable[i, 0], messageTable[i, 1], targetMethod)
                            : DelegateBuilderHandlerExtendedFunc.Build<T>(
                                messageTable[i, 0], messageTable[i, 1], targetMethod);
                    RequestHandler<T> handler =
                        (self, requestMessage) => invokeDelegate(self, requestMessage.InvokePayload);

                    if (handlerBuilder != null)
                    {
                        handler = (RequestHandler<T>)handlerBuilder.Invoke(null, new object[] { handler, targetMethod });
                    }

                    _handlerTable[invokeMessageType] = new RequestHandlerInfo
                    {
                        InterfaceType = ifs,
                        IsReentrant = isReentrant,
                        Handler = handler
                    };
                }
            }
            if (targetMethods.Any())
            {
                throw new InvalidOperationException(
                    $"{typeof(T).FullName} has dangling handlers.\n" +
                    string.Join("\n", targetMethods.Select(x => x.Item1.Name)));
            }
        }

        private static Type[,] GetInterfacePayloadTypeTable(Type interfaceType)
        {
            var payloadTableType =
                interfaceType.Assembly.GetTypes()
                             .Where(t =>
                             {
                                 var attr = t.GetCustomAttribute<PayloadTableForInterfacedActorAttribute>();
                                 return (attr != null && attr.Type == interfaceType);
                             })
                             .FirstOrDefault();

            if (payloadTableType == null)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot find payload table class for {0}", interfaceType.FullName));
            }

            var queryMethodInfo = payloadTableType.GetMethod("GetPayloadTypes");
            if (queryMethodInfo == null)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot find {0}.GetPayloadTypes method", payloadTableType.FullName));
            }

            var payloadTypes = (Type[,])queryMethodInfo.Invoke(null, new object[] { });
            if (payloadTypes == null || payloadTypes.GetLength(0) != interfaceType.GetMethods().Length)
            {
                throw new InvalidOperationException(
                    string.Format("Mismatched messageTable from {0}", payloadTableType.FullName));
            }

            return payloadTypes;
        }

        private static bool AreParameterTypesEqual(ParameterInfo[] a, ParameterInfo[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].ParameterType != b[i].ParameterType)
                    return false;
            }
            return true;
        }

        public RequestHandlerInfo GetRequestHandler(Type type)
        {
            RequestHandlerInfo info;
            return _handlerTable.TryGetValue(type, out info) ? info : null;
        }
    }
}
