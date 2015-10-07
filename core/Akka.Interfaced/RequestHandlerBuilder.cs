using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced
{
    public class RequestHandlerBuilder<T> where T : class
    {
        private Dictionary<Type, RequestHandlerItem<T>> _table;

        private class FilterItem
        {
            public FilterItem(IFilterFactory factory, IFilter referenceFilter, FilterAccessor accessor, int filterPerInvokeIndex = -1)
            {
                Factory = factory;
                Accessor = accessor;

                Order = referenceFilter.Order;
                IsAsync = referenceFilter is IPreHandleAsyncFilter ||
                          referenceFilter is IPostHandleAsyncFilter;
                IsPreHandleFilter = referenceFilter is IPreHandleFilter ||
                                    referenceFilter is IPreHandleAsyncFilter;
                IsPostHandleFilter = referenceFilter is IPostHandleFilter ||
                                     referenceFilter is IPostHandleAsyncFilter;
                IsPerInstance = factory is IFilterPerInstanceFactory ||
                                factory is IFilterPerInstanceMethodFactory;
                IsPerInvoke = factory is IFilterPerInvokeFactory;
                FilterPerInvokeIndex = FilterPerInvokeIndex;
            }

            public IFilterFactory Factory { get; }
            public FilterAccessor Accessor { get; }
            public int Order { get; }
            public bool IsAsync { get; }
            public bool IsPreHandleFilter { get; }
            public bool IsPostHandleFilter { get; }
            public bool IsPerInstance { get; }
            public bool IsPerInvoke { get; }
            public int FilterPerInvokeIndex { get; }
        }

        private Dictionary<Type, FilterItem> _perClassFilterItemTable;
        private List<Func<object, IFilter>> _perInstanceFilterCreators;

        public Dictionary<Type, RequestHandlerItem<T>> HandlerTable => _table;
        public List<Func<object, IFilter>> PerInstanceFilterCreators => _perInstanceFilterCreators;

        public void Build()
        {
            _table = new Dictionary<Type, RequestHandlerItem<T>>();
            _perClassFilterItemTable = new Dictionary<Type, FilterItem>();
            _perInstanceFilterCreators = new List<Func<object, IFilter>>();

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
                var methodItems = interfaceMap.InterfaceMethods.Zip(interfaceMap.TargetMethods, Tuple.Create)
                                              .OrderBy(p => p.Item1, new MethodInfoComparer())
                                              .ToArray();
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);

                for (var i = 0; i < methodItems.Length; i++)
                {
                    var targetMethod = methodItems[i].Item2;
                    var invokePayloadType = payloadTypeTable[i, 0];
                    var returnPayloadType = payloadTypeTable[i, 1];

                    var filters = CreateFilters(type, targetMethod);
                    var preHandleFilterItems = filters.Item1;
                    var postHandleFilterItems = filters.Item2;

                    var asyncHandler = BuildAsyncHandler(
                        invokePayloadType, returnPayloadType, targetMethod,
                        preHandleFilterItems, postHandleFilterItems);

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
                    var preHandleFilterItems = filters.Item1;
                    var postHandleFilterItems = filters.Item2;

                    if (isAsyncMethod ||
                        preHandleFilterItems.Any(f => f.IsAsync) ||
                        postHandleFilterItems.Any(f => f.IsAsync))
                    {
                        // async handler

                        var asyncHandler = BuildAsyncHandler(
                            invokePayloadType, returnPayloadType, targetMethod,
                            preHandleFilterItems, postHandleFilterItems);

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
                            preHandleFilterItems, postHandleFilterItems);

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

        private Tuple<List<FilterItem>, List<FilterItem>> CreateFilters(Type type, MethodInfo method)
        {
            var preHandleFilterItems = new List<FilterItem>();
            var postHandleFilterItems = new List<FilterItem>();

            var filterPerInvokeIndex = 0;
            var filterFactories = type.GetCustomAttributes().Concat(method.GetCustomAttributes()).OfType<IFilterFactory>();
            foreach (var filterFactory in filterFactories)
            {
                // create filter with factory

                FilterItem filterItem = null;
                if (filterFactory is IFilterPerClassFactory)
                {
                    var factoryType = filterFactory.GetType();
                    if (_perClassFilterItemTable.TryGetValue(factoryType, out filterItem) == false)
                    {
                        var factory = (IFilterPerClassFactory)filterFactory;
                        factory.Setup(type);
                        var filter = factory.CreateInstance();
                        filterItem = new FilterItem(filterFactory, filter, (_, __) => filter);
                        _perClassFilterItemTable.Add(factoryType, filterItem);
                    }
                }
                else if (filterFactory is IFilterPerClassMethodFactory)
                {
                    var factory = (IFilterPerClassMethodFactory)filterFactory;
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance();
                    filterItem = new FilterItem(filterFactory, filter, (_, __) => filter);
                }
                else if (filterFactory is IFilterPerInstanceFactory)
                {
                    var factoryType = filterFactory.GetType();
                    if (_perClassFilterItemTable.TryGetValue(factoryType, out filterItem) == false)
                    {
                        var factory = ((IFilterPerInstanceFactory)filterFactory);
                        factory.Setup(type);
                        var filter = factory.CreateInstance(null);
                        var arrayIndex = _perInstanceFilterCreators.Count;
                        _perInstanceFilterCreators.Add(a => factory.CreateInstance(a));
                        filterItem = new FilterItem(filterFactory, filter,
                                                    (provider, __) => provider.GetFilter(arrayIndex));
                        _perClassFilterItemTable.Add(factoryType, filterItem);
                    }
                }
                else if (filterFactory is IFilterPerInstanceMethodFactory)
                {
                    var factory = ((IFilterPerInstanceMethodFactory)filterFactory);
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance(null);
                    var arrayIndex = _perInstanceFilterCreators.Count;
                    _perInstanceFilterCreators.Add(a => factory.CreateInstance(a));
                    filterItem = new FilterItem(filterFactory, filter,
                                                (provider, __) => provider.GetFilter(arrayIndex));
                }
                else if (filterFactory is IFilterPerInvokeFactory)
                {
                    var factory = ((IFilterPerInvokeFactory)filterFactory);
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance(null, null);
                    var arrayIndex = filterPerInvokeIndex++;
                    filterItem = new FilterItem(filterFactory, filter,
                                                (_, filters) => filters[arrayIndex], filterPerInvokeIndex);
                }

                // classify filter and add it to list
                // beware that a filter can be added to both pre and post handle filter list.

                if (filterItem.IsPreHandleFilter)
                    preHandleFilterItems.Add(filterItem);

                if (filterItem.IsPostHandleFilter)
                    postHandleFilterItems.Add(filterItem);
            }

            return Tuple.Create(preHandleFilterItems.OrderBy(f => f.Order).ToList(),
                                postHandleFilterItems.OrderByDescending(f => f.Order).ToList());
        }

        private static RequestHandler<T> BuildHandler(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method,
            IList<FilterItem> preHandleFilterItems, IList<FilterItem> postHandleFilterItems)
        {
            var handler = RequestHandlerFuncBuilder.Build<T>(
                invokePayloadType, returnPayloadType, method);

            var allFilters = preHandleFilterItems.Concat(postHandleFilterItems).ToList();
            var perInstanceFilterExists = allFilters.Any(i => i.IsPerInstance);
            var perInvokeFilterFactories = allFilters.Where(i => i.IsPerInvoke).GroupBy(i => i.FilterPerInvokeIndex)
                                                     .OrderBy(g => g.Key).Select(g => (IFilterPerInvokeFactory)g.Last().Factory).ToArray();
            var preHandleFilterAccessors = preHandleFilterItems.Select(i => i.Accessor).ToArray();
            var postHandleFilterAccessors = postHandleFilterItems.Select(i => i.Accessor).ToArray();

            return delegate (T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                var filterPerInstanceProvider = perInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create perInvoke filters

                IFilter[] filterPerInvokes = null;
                if (perInvokeFilterFactories.Length > 0)
                {
                    filterPerInvokes = new IFilter[perInvokeFilterFactories.Length];
                    for (var i = 0; i < perInvokeFilterFactories.Length; i++)
                    {
                        filterPerInvokes[i] = perInvokeFilterFactories[i].CreateInstance(self, request);
                    }
                }

                // Call PreHandleFilters

                if (preHandleFilterItems.Count > 0)
                {
                    var context = new PreHandleFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filterAccessor in preHandleFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerInvokes);
                            ((IPreHandleFilter)filter).OnPreHandle(context);
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

                // Call PostHandleFilters

                if (postHandleFilterItems.Count > 0)
                {
                    var context = new PostHandleFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filterAccessor in postHandleFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerInvokes);
                            ((IPostHandleFilter)filter).OnPostHandle(context);
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
            Type invokePayloadType, Type returnPayloadType, MethodInfo method,
            IList<FilterItem> preHandleFilterItems, IList<FilterItem> postHandleFilterItems) 
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? RequestHandlerAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method)
                : RequestHandlerSyncToAsyncBuilder.Build<T>(invokePayloadType, returnPayloadType, method);

            var allFilters = preHandleFilterItems.Concat(postHandleFilterItems).ToList();
            var perInstanceFilterExists = allFilters.Any(i => i.IsPerInstance);
            var perInvokeFilterFactories = allFilters.Where(i => i.IsPerInvoke).GroupBy(i => i.FilterPerInvokeIndex)
                                                     .OrderBy(g => g.Key).Select(g => (IFilterPerInvokeFactory)g.Last().Factory).ToArray();
            var preHandleFilterAccessors = preHandleFilterItems.Select(i => i.Accessor).ToArray();
            var postHandleFilterAccessors = postHandleFilterItems.Select(i => i.Accessor).ToArray();

            // TODO: Optimize this function when without async filter
            return async delegate(T self, RequestMessage request, Action<ResponseMessage> onCompleted)
            {
                ResponseMessage response = null;

                var filterPerInstanceProvider = perInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create perInvoke filters

                IFilter[] filterPerInvokes = null;
                if (perInvokeFilterFactories.Length > 0)
                {
                    filterPerInvokes = new IFilter[perInvokeFilterFactories.Length];
                    for (var i = 0; i < perInvokeFilterFactories.Length; i++)
                    {
                        filterPerInvokes[i] = perInvokeFilterFactories[i].CreateInstance(self, request);
                    }
                }

                // Call PreHandleFilters

                if (preHandleFilterItems.Count > 0)
                {
                    var context = new PreHandleFilterContext
                    {
                        Actor = self,
                        Request = request
                    };
                    foreach (var filterAccessor in preHandleFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerInvokes);
                            var preHandleFilter = filter as IPreHandleFilter;
                            if (preHandleFilter != null)
                                preHandleFilter.OnPreHandle(context);
                            else
                                await ((IPreHandleAsyncFilter)filter).OnPreHandleAsync(context);

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

                // Call PostHandleFilters

                if (postHandleFilterItems.Count > 0)
                {
                    var context = new PostHandleFilterContext
                    {
                        Actor = self,
                        Request = request,
                        Response = response
                    };
                    foreach (var filterAccessor in postHandleFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerInvokes);
                            var postHandleFilter = filter as IPostHandleFilter;
                            if (postHandleFilter != null)
                                postHandleFilter.OnPostHandle(context);
                            else
                                await ((IPostHandleAsyncFilter)filter).OnPostHandleAsync(context);
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
