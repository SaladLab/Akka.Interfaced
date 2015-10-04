using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced
{
    public class RequestHandlerBuilder<T> where T : class
    {
        private Dictionary<Type, RequestHandlerItem<T>> _table;
        private Dictionary<Type, IFilter> _filterInClassTable;

        public Dictionary<Type, RequestHandlerItem<T>> BuildTable()
        {
            _table = new Dictionary<Type, RequestHandlerItem<T>>();
            _filterInClassTable = new Dictionary<Type, IFilter>();

            BuildRegularInterfaceHandler();
            BuildExtendedInterfaceHandler();

            return _table;
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

                    var filters = GetFilters(type, targetMethod);
                    var preHandleFilters = filters.Item1;
                    var postHandleFilters = filters.Item2;

                    var asyncHandler = BuildAsyncHandler(
                        invokePayloadType, returnPayloadType, targetMethod,
                        preHandleFilters, postHandleFilters);

                    _table.Add(invokePayloadType, new RequestHandlerItem<T>
                    {
                        InterfaceType = ifs,
                        IsReentrant = IsReentrantMethod(targetMethod),
                        AsyncHandler = asyncHandler
                    });
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
                    var filters = GetFilters(type, targetMethod);
                    var preHandleFilters = filters.Item1;
                    var postHandleFilters = filters.Item2;

                    if (isAsyncMethod ||
                        preHandleFilters.Any(f => f is IPreHandleAsyncFilter) ||
                        postHandleFilters.Any(f => f is IPostHandleAsyncFilter))
                    {
                        // async handler

                        var asyncHandler = BuildAsyncHandler(
                            invokePayloadType, returnPayloadType, targetMethod,
                            preHandleFilters, postHandleFilters);

                        _table.Add(invokePayloadType, new RequestHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = IsReentrantMethod(targetMethod),
                            AsyncHandler = asyncHandler
                        });
                    }
                    else
                    {
                        // sync handler

                        var handler = BuildHandler(
                            invokePayloadType, returnPayloadType, targetMethod,
                            preHandleFilters.Cast<IPreHandleFilter>().ToList(),
                            postHandleFilters.Cast<IPostHandleFilter>().ToList());

                        _table.Add(invokePayloadType, new RequestHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = handler
                        });
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

        private Tuple<List<IFilter>, List<IFilter>> GetFilters(Type type, MethodInfo method)
        {
            var preHandleFilters = new List<IFilter>();
            var postHandleFilters = new List<IFilter>();

            foreach (var filterFactory in type.GetCustomAttributes().OfType<IFilterFactory>().Concat(
                                          method.GetCustomAttributes().OfType<IFilterFactory>()))
            {
                // create filter with factory

                IFilter filter = null;
                if (filterFactory is IFilterPerClassFactory)
                {
                    var factoryType = filterFactory.GetType();
                    if (_filterInClassTable.ContainsKey(factoryType) == false)
                    {
                        _filterInClassTable[factoryType] =
                            ((IFilterPerClassFactory)filterFactory).CreateInstance(type);
                    }
                    filter = _filterInClassTable[factoryType];
                }
                else if (filterFactory is IFilterPerClassMethodFactory)
                {
                    filter = ((IFilterPerClassMethodFactory)filterFactory).CreateInstance(type, method);
                }

                // classify filter and add it to list
                // beware that a filter can be added to both pre and post handle filter list.

                if (filter is IPreHandleFilter || filter is IPreHandleAsyncFilter)
                    preHandleFilters.Add(filter);

                if (filter is IPostHandleFilter || filter is IPostHandleAsyncFilter)
                    postHandleFilters.Add(filter);
            }

            return Tuple.Create(preHandleFilters.OrderBy(f => f.Order).ToList(),
                                postHandleFilters.OrderByDescending(f => f.Order).ToList());
        }

        private static RequestHandler<T> BuildHandler(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method,
            IList<IPreHandleFilter> preHandleFilters, IList<IPostHandleFilter> postHandleFilters)
        {
            var handler = RequestHandlerFuncBuilder.Build<T>(
                invokePayloadType, returnPayloadType, method);

            return delegate (T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                // Call PreHandleFilters

                if (preHandleFilters.Count > 0)
                {
                    var context = new PreHandleFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filter in preHandleFilters)
                    {
                        try
                        {
                            filter.OnPreHandle(context);
                            if (context.Response != null)
                            {
                                response = context.Response;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                // Call Handler

                if (response == null)
                {
                    try
                    {
                        var returnPayload = handler(self, request.InvokePayload);
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            ReturnPayload = returnPayload
                        };
                    }
                    catch (Exception e)
                    {
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            Exception = e
                        };
                    }
                }

                // Call PostHandleFilters

                if (postHandleFilters.Count > 0)
                {
                    var context = new PostHandleFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filter in postHandleFilters)
                    {
                        try
                        {
                            filter.OnPostHandle(context);
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                if (onCompleted != null)
                    onCompleted(response);

                return response;
            };
        }

        private static RequestAsyncHandler<T> BuildAsyncHandler(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method,
            IList<IFilter> preHandleFilters, IList<IFilter> postHandleFilters) 
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? RequestHandlerAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method)
                : RequestHandlerSyncToAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method);

            // TODO: Optimize this function when without async filter
            return async delegate(T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                // Call PreHandleFilters

                if (preHandleFilters.Count > 0)
                {
                    var context = new PreHandleFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filter in preHandleFilters)
                    {
                        try
                        {
                            var preFilter = filter as IPreHandleFilter;
                            if (preFilter != null)
                            {
                                preFilter.OnPreHandle(context);
                            }
                            else
                            {
                                var preAsyncFilter = filter as IPreHandleAsyncFilter;
                                if (preAsyncFilter != null)
                                {
                                    await preAsyncFilter.OnPreHandleAsync(context);
                                }
                            }

                            if (context.Response != null)
                            {
                                response = context.Response;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                // Call Handler

                if (response == null)
                {
                    try
                    {
                        var returnPayload = await handler(self, request.InvokePayload);
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            ReturnPayload = returnPayload
                        };
                    }
                    catch (Exception e)
                    {
                        response = new ResponseMessage
                        {
                            RequestId = request.RequestId,
                            Exception = e
                        };
                    }
                }

                // Call PostHandleFilters

                if (postHandleFilters.Count > 0)
                {
                    var context = new PostHandleFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filter in postHandleFilters)
                    {
                        try
                        {
                            var postFilter = filter as IPostHandleFilter;
                            if (postFilter != null)
                            {
                                postFilter.OnPostHandle(context);
                            }
                            else
                            {
                                var postAsyncFilter = filter as IPostHandleAsyncFilter;
                                if (postAsyncFilter != null)
                                {
                                    await postAsyncFilter.OnPostHandleAsync(context);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                        }
                    }
                }

                if (onCompleted != null)
                    onCompleted(response);

                return response;
            };
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
    }
}
