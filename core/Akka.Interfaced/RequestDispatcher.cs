using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced
{
    public class RequestDispatcher<T> where T : class
    {
        public class RequestHandlerInfo
        {
            public Type InterfaceType;
            public bool IsReentrant;
            public RequestHandler<T> Handler;
            public RequestAsyncHandler<T> AsyncHandler;
        }
        private Dictionary<Type, RequestHandlerInfo> _handlerTable;

        public RequestDispatcher()
        {
            BuildTable();
        }
            
        private void BuildTable()
        {
            _handlerTable = new Dictionary<Type, RequestHandlerInfo>();

            BuildRegularInterfaceHandler();
            BuildExtendedInterfaceHandler();
        }

        private void BuildRegularInterfaceHandler()
        {
            var type = typeof(T);

            foreach (var ifs in type.GetInterfaces())
            {
                if (ifs.GetInterfaces().All(t => t != typeof(IInterfacedActor)))
                    continue;

                var interfaceMap = type.GetInterfaceMap(ifs);
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);

                for (var i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
                {
                    var targetMethod = interfaceMap.TargetMethods[i];
                    var invokePayloadType = payloadTypeTable[i, 0];
                    var returnPayloadType = payloadTypeTable[i, 1];

                    var filters = CreateFilters(type, targetMethod);
                    var preHandleFilters = filters.Item1;
                    var postHandleFilters = filters.Item2;

                    var asyncHandler = RequestHandlerBuilder.BuildAsyncHandler<T>(
                        invokePayloadType, returnPayloadType, targetMethod,
                        preHandleFilters, postHandleFilters);

                    _handlerTable[invokePayloadType] = new RequestHandlerInfo
                    {
                        InterfaceType = ifs,
                        IsReentrant = IsReentrantMethod(targetMethod),
                        AsyncHandler = asyncHandler
                    };
                }
            }
        }

        private void BuildExtendedInterfaceHandler()
        {
            var type = typeof(T);

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
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);
                var interfaceMethods = ifs.GetMethods();

                for (var i = 0; i < interfaceMethods.Length; i++)
                {
                    var interfaceMethod = interfaceMethods[i];
                    var invokePayloadType = payloadTypeTable[i, 0];
                    var returnPayloadType = payloadTypeTable[i, 1];
                    var name = interfaceMethod.Name;
                    var parameters = interfaceMethod.GetParameters();

                    // find a method which can handle this invoke payload

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

                    // build handler

                    var isAsyncMethod = targetMethod.ReturnType.Name.StartsWith("Task");
                    var filters = CreateFilters(type, targetMethod);
                    var preHandleFilters = filters.Item1;
                    var postHandleFilters = filters.Item2;

                    if (isAsyncMethod ||
                        preHandleFilters.Any(f => f is IPreHandleAsyncFilter) ||
                        postHandleFilters.Any(f => f is IPostHandleAsyncFilter))
                    {
                        // Async handler!

                        var asyncHandler = RequestHandlerBuilder.BuildAsyncHandler<T>(
                            invokePayloadType, returnPayloadType, targetMethod,
                            preHandleFilters, postHandleFilters);

                        _handlerTable[invokePayloadType] = new RequestHandlerInfo
                        {
                            InterfaceType = ifs,
                            IsReentrant = IsReentrantMethod(targetMethod),
                            AsyncHandler = asyncHandler
                        };
                    }
                    else
                    {
                        // Sync handler

                        var handler = RequestHandlerBuilder.BuildHandler<T>(
                            invokePayloadType, returnPayloadType, targetMethod,
                            preHandleFilters.Cast<IPreHandleFilter>().ToList(),
                            postHandleFilters.Cast<IPostHandleFilter>().ToList());

                        _handlerTable[invokePayloadType] = new RequestHandlerInfo
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = handler
                        };
                    }
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

        private static bool IsReentrantMethod(MethodInfo method)
        {
            return method.CustomAttributes.Any(x => x.AttributeType == typeof(ReentrantAttribute));
        }

        private static Tuple<List<IFilter>, List<IFilter>> CreateFilters(Type type, MethodInfo method)
        {
            var preHandleFilters = new List<IFilter>();
            var postHandleFilters = new List<IFilter>();

            foreach (var filterFactory in type.GetCustomAttributes().OfType<IFilterFactory>().Concat(
                                          method.GetCustomAttributes().OfType<IFilterFactory>()))
            {
                var filter = filterFactory.CreateInstance(typeof(T), method);

                if (filter is IPreHandleFilter || filter is IPreHandleAsyncFilter)
                    preHandleFilters.Add(filter);

                if (filter is IPostHandleFilter || filter is IPostHandleAsyncFilter)
                    postHandleFilters.Add(filter);
            }

            return Tuple.Create(preHandleFilters.OrderBy(f => f.Order).ToList(), 
                                postHandleFilters.OrderByDescending(f => f.Order).ToList());
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
