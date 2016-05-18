using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable SA1130 // Use lambda syntax

namespace Akka.Interfaced
{
    internal class RequestHandlerBuilder<T>
        where T : class
    {
        private Dictionary<Type, RequestHandlerItem<T>> _table;
        private FilterHandlerBuilder _filterHandlerBuilder;

        public Dictionary<Type, RequestHandlerItem<T>> Build(FilterHandlerBuilder filterHandlerBuilder)
        {
            _table = new Dictionary<Type, RequestHandlerItem<T>>();
            _filterHandlerBuilder = filterHandlerBuilder;

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
                var methodItems = interfaceMap.InterfaceMethods.Zip(interfaceMap.TargetMethods, Tuple.Create)
                                              .OrderBy(p => p.Item1, new MethodInfoComparer())
                                              .ToArray();
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);

                for (var i = 0; i < methodItems.Length; i++)
                {
                    var targetMethod = methodItems[i].Item2;
                    var invokePayloadType = payloadTypeTable[i, 0];
                    var returnPayloadType = payloadTypeTable[i, 1];
                    var filterChain = _filterHandlerBuilder.Build(targetMethod, FilterChainKind.Request);
                    var asyncHandler = BuildAsyncHandler(invokePayloadType, returnPayloadType, targetMethod, filterChain);

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
                    .SelectMany(t => t.GenericTypeArguments)
                    .Where(t => t.GetInterfaces().Any(i => i == typeof(IInterfacedActor)));

            foreach (var ifs in extendedInterfaces)
            {
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);
                var interfaceMethods = ifs.GetMethods().OrderBy(m => m, new MethodInfoComparer()).ToArray();

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
                        if (method.Item2.Type != null || method.Item2.Method != null)
                        {
                            // check tagged method
                            if (method.Item2.Type != null && method.Item2.Type != ifs)
                                continue;
                            if (method.Item2.Method != null && method.Item2.Method != name)
                                continue;
                        }
                        else if (method.Item1.Name != name)
                        {
                            // check method
                            continue;
                        }

                        if (AreParameterTypesEqual(method.Item1.GetParameters(), parameters))
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
                    var filterChain = _filterHandlerBuilder.Build(targetMethod, FilterChainKind.Request);

                    if (isAsyncMethod || filterChain.AsyncFilterExists)
                    {
                        _table.Add(invokePayloadType, new RequestHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = IsReentrantMethod(targetMethod),
                            AsyncHandler = BuildAsyncHandler(invokePayloadType, returnPayloadType, targetMethod, filterChain)
                        });
                    }
                    else
                    {
                        if (targetMethod.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                            throw new InvalidOperationException($"Async void handler is not supported. ({type.FullName}.{targetMethod.Name})");

                        _table.Add(invokePayloadType, new RequestHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = BuildHandler(invokePayloadType, returnPayloadType, targetMethod, filterChain)
                        });
                    }
                }
            }
        }

        private static RequestHandler<T> BuildHandler(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method, FilterChain filterChain)
        {
            var handler = RequestHandlerFuncBuilder.Build<T>(invokePayloadType, returnPayloadType, method);

            return delegate(T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                var filterPerInstanceProvider = filterChain.PerInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create PerRequest filters

                IFilter[] filterPerRequests = null;
                if (filterChain.PerInvokeFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[filterChain.PerInvokeFilterFactories.Length];
                    for (var i = 0; i < filterChain.PerInvokeFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = filterChain.PerInvokeFilterFactories[i].CreateInstance(self, request);
                    }
                }

                // Call PreFilters

                if (filterChain.PreFilterAccessors.Length > 0)
                {
                    var context = new PreRequestFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filterAccessor in filterChain.PreFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            ((IPreRequestFilter)filter).OnPreRequest(context);
                            if (context.Response != null)
                            {
                                response = context.Response;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                            Console.WriteLine(e);
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

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostRequestFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            ((IPostRequestFilter)filter).OnPostRequest(context);
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                            Console.WriteLine(e);
                        }
                    }
                }

                if (onCompleted != null)
                    onCompleted(response);

                return response;
            };
        }

        private static RequestAsyncHandler<T> BuildAsyncHandler(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method, FilterChain filterChain)
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? RequestHandlerAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method)
                : RequestHandlerSyncToAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method);

            // TODO: Optimize this function when without async filter
            return async delegate(T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                var filterPerInstanceProvider = filterChain.PerInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create PerRequest filters

                IFilter[] filterPerRequests = null;
                if (filterChain.PerInvokeFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[filterChain.PerInvokeFilterFactories.Length];
                    for (var i = 0; i < filterChain.PerInvokeFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = filterChain.PerInvokeFilterFactories[i].CreateInstance(self, request);
                    }
                }

                // Call PreFilters

                if (filterChain.PreFilterAccessors.Length > 0)
                {
                    var context = new PreRequestFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filterAccessor in filterChain.PreFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            var preFilter = filter as IPreRequestFilter;
                            if (preFilter != null)
                                preFilter.OnPreRequest(context);
                            else
                                await ((IPreRequestAsyncFilter)filter).OnPreRequestAsync(context);

                            if (context.Response != null)
                            {
                                response = context.Response;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                            Console.WriteLine(e);
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

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostRequestFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            var postFilter = filter as IPostRequestFilter;
                            if (postFilter != null)
                                postFilter.OnPostRequest(context);
                            else
                                await ((IPostRequestAsyncFilter)filter).OnPostRequestAsync(context);
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                            Console.WriteLine(e);
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
                                 var attr = t.GetCustomAttribute<PayloadTableAttribute>();
                                 return (attr != null && attr.Type == interfaceType && attr.Kind == PayloadTableKind.Request);
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
