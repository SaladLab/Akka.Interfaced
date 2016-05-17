using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable SA1130 // Use lambda syntax

namespace Akka.Interfaced
{
    public class NotificationHandlerBuilder<T>
        where T : class
    {
        private Dictionary<Type, NotificationHandlerItem<T>> _table;

        private class FilterItem
        {
            public FilterItem(IFilterFactory factory, IFilter referenceFilter, FilterAccessor accessor, int filterPerNotificationIndex = -1)
            {
                Factory = factory;
                Accessor = accessor;

                Order = referenceFilter.Order;
                IsAsync = referenceFilter is IPreNotificationAsyncFilter ||
                          referenceFilter is IPostNotificationAsyncFilter;
                IsPreFilter = referenceFilter is IPreNotificationFilter ||
                              referenceFilter is IPreNotificationAsyncFilter;
                IsPostFilter = referenceFilter is IPostNotificationFilter ||
                               referenceFilter is IPostNotificationAsyncFilter;
                IsPerInstance = factory is IFilterPerInstanceFactory ||
                                factory is IFilterPerInstanceMethodFactory;
                IsPerNotification = factory is IFilterPerNotificationFactory;
                FilterPerNotificationIndex = filterPerNotificationIndex;
            }

            public IFilterFactory Factory { get; }
            public FilterAccessor Accessor { get; }
            public int Order { get; }
            public bool IsAsync { get; }
            public bool IsPreFilter { get; }
            public bool IsPostFilter { get; }
            public bool IsPerInstance { get; }
            public bool IsPerNotification { get; }
            public int FilterPerNotificationIndex { get; }
        }

        private Dictionary<Type, FilterItem> _perClassFilterItemTable;
        private List<Func<object, IFilter>> _perInstanceFilterCreators;

        public Dictionary<Type, NotificationHandlerItem<T>> HandlerTable => _table;
        public List<Func<object, IFilter>> PerInstanceFilterCreators => _perInstanceFilterCreators;

        public void Build()
        {
            _table = new Dictionary<Type, NotificationHandlerItem<T>>();
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
                if (ifs.GetInterfaces().All(t => t != typeof(IInterfacedObserver)))
                    continue;

                var interfaceMap = type.GetInterfaceMap(ifs);
                var methodItems = interfaceMap.InterfaceMethods.Zip(interfaceMap.TargetMethods, Tuple.Create)
                                              .OrderBy(p => p.Item1, new MethodInfoComparer())
                                              .ToArray();
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);

                for (var i = 0; i < methodItems.Length; i++)
                {
                    var targetMethod = methodItems[i].Item2;
                    var invokePayloadType = payloadTypeTable[i];

                    // build handler

                    var filters = CreateFilters(type, targetMethod);
                    var preFilterItems = filters.Item1;
                    var postFilterItems = filters.Item2;

                    if (preFilterItems.Any(f => f.IsAsync) ||
                        postFilterItems.Any(f => f.IsAsync))
                    {
                        // async handler

                        var asyncHandler = BuildAsyncHandler(
                                invokePayloadType, targetMethod,
                                preFilterItems, postFilterItems);

                        _table.Add(invokePayloadType, new NotificationHandlerItem<T>
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
                            invokePayloadType, targetMethod,
                            preFilterItems, postFilterItems);

                        _table.Add(invokePayloadType, new NotificationHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = handler
                        });
                    }
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
                    .Where(t => t.GetInterfaces().Any(i => i == typeof(IInterfacedObserver)));

            foreach (var ifs in extendedInterfaces)
            {
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);
                var interfaceMethods = ifs.GetMethods().OrderBy(m => m, new MethodInfoComparer()).ToArray();

                for (var i = 0; i < interfaceMethods.Length; i++)
                {
                    var interfaceMethod = interfaceMethods[i];
                    var invokePayloadType = payloadTypeTable[i];
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
                    var filters = CreateFilters(type, targetMethod);
                    var preFilterItems = filters.Item1;
                    var postFilterItems = filters.Item2;

                    if (isAsyncMethod ||
                        preFilterItems.Any(f => f.IsAsync) ||
                        postFilterItems.Any(f => f.IsAsync))
                    {
                        // async handler

                        var asyncHandler = BuildAsyncHandler(
                            invokePayloadType, targetMethod,
                            preFilterItems, postFilterItems);

                        _table.Add(invokePayloadType, new NotificationHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = IsReentrantMethod(targetMethod),
                            AsyncHandler = asyncHandler
                        });
                    }
                    else
                    {
                        if (targetMethod.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                            throw new InvalidOperationException($"Async void handler is not supported. ({type.FullName}.{targetMethod.Name})");

                        // sync handler

                        var handler = BuildHandler(
                            invokePayloadType, targetMethod,
                            preFilterItems, postFilterItems);

                        _table.Add(invokePayloadType, new NotificationHandlerItem<T>
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = handler
                        });
                    }
                }
            }
        }

        private Tuple<List<FilterItem>, List<FilterItem>> CreateFilters(Type type, MethodInfo method)
        {
            var preFilterItems = new List<FilterItem>();
            var postFilterItems = new List<FilterItem>();

            var filterPerRequestIndex = 0;
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
                        filterItem = new FilterItem(filterFactory, filter, (_, x) => filter);
                        _perClassFilterItemTable.Add(factoryType, filterItem);
                    }
                }
                else if (filterFactory is IFilterPerClassMethodFactory)
                {
                    var factory = (IFilterPerClassMethodFactory)filterFactory;
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance();
                    filterItem = new FilterItem(filterFactory, filter, (_, x) => filter);
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
                                                    (provider, _) => provider.GetFilter(arrayIndex));
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
                                                (provider, _) => provider.GetFilter(arrayIndex));
                }
                else if (filterFactory is IFilterPerNotificationFactory)
                {
                    var factory = ((IFilterPerNotificationFactory)filterFactory);
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance(null, null);
                    var arrayIndex = filterPerRequestIndex++;
                    filterItem = new FilterItem(filterFactory, filter,
                                                (_, filters) => filters[arrayIndex], filterPerRequestIndex);
                }

                // classify filter and add it to list
                // beware that a filter can be added to both pre and post handle filter list.

                if (filterItem.IsPreFilter)
                    preFilterItems.Add(filterItem);

                if (filterItem.IsPostFilter)
                    postFilterItems.Add(filterItem);
            }

            return Tuple.Create(preFilterItems.OrderBy(f => f.Order).ToList(),
                                postFilterItems.OrderByDescending(f => f.Order).ToList());
        }

        private static NotificationHandler<T> BuildHandler(
            Type invokePayloadType, MethodInfo method,
            IList<FilterItem> preFilterItems, IList<FilterItem> postFilterItems)
        {
            var handler = RequestHandlerFuncBuilder.Build<T>(
                invokePayloadType, null, method);

            var allFilters = preFilterItems.Concat(postFilterItems).ToList();
            var perInstanceFilterExists = allFilters.Any(i => i.IsPerInstance);
            var perNotificationFilterFactories = allFilters.Where(i => i.IsPerNotification).GroupBy(i => i.FilterPerNotificationIndex)
                                                           .OrderBy(g => g.Key).Select(g => (IFilterPerNotificationFactory)g.Last().Factory).ToArray();
            var preFilterAccessors = preFilterItems.Select(i => i.Accessor).ToArray();
            var postFilterAccessors = postFilterItems.Select(i => i.Accessor).ToArray();

            return delegate(T self, NotificationMessage notification)
            {
                ResponseMessage response = null;

                var filterPerInstanceProvider = perInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create perRequest filters

                IFilter[] filterPerRequests = null;

                if (perNotificationFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[perNotificationFilterFactories.Length];
                    for (var i = 0; i < perNotificationFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = perNotificationFilterFactories[i].CreateInstance(self, notification);
                    }
                }

                // Call PreHandleFilters

                if (preFilterItems.Count > 0)
                {
                    var context = new PreNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification
                    };
                    foreach (var filterAccessor in preFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            ((IPreNotificationFilter)filter).OnPreNotification(context);
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
                        handler(self, notification.InvokePayload);
                    }
                    catch (Exception e)
                    {
                        // TODO: Exception Handling
                    }
                }

                // Call PostHandleFilters

                if (postFilterItems.Count > 0)
                {
                    var context = new PostNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification
                    };
                    foreach (var filterAccessor in postFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            ((IPostNotificationFilter)filter).OnPostNotification(context);
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                            Console.WriteLine(e);
                        }
                    }
                }
            };
        }

        private static NotificationAsyncHandler<T> BuildAsyncHandler(
            Type invokePayloadType, MethodInfo method,
            IList<FilterItem> preFilterItems, IList<FilterItem> postFilterItems)
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? RequestHandlerAsyncBuilder.Build<T>(invokePayloadType, null, method)
                : RequestHandlerSyncToAsyncBuilder.Build<T>(invokePayloadType, null, method);

            var allFilters = preFilterItems.Concat(postFilterItems).ToList();
            var perInstanceFilterExists = allFilters.Any(i => i.IsPerInstance);
            var perNotificationFilterFactories = allFilters.Where(i => i.IsPerNotification).GroupBy(i => i.FilterPerNotificationIndex)
                                                           .OrderBy(g => g.Key).Select(g => (IFilterPerNotificationFactory)g.Last().Factory).ToArray();
            var preFilterAccessors = preFilterItems.Select(i => i.Accessor).ToArray();
            var postFilterAccessors = postFilterItems.Select(i => i.Accessor).ToArray();

            // TODO: Optimize this function when without async filter
            return async delegate(T self, NotificationMessage notification)
            {
                ResponseMessage response = null;

                var filterPerInstanceProvider = perInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create perRequest filters

                IFilter[] filterPerRequests = null;
                if (perNotificationFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[perNotificationFilterFactories.Length];
                    for (var i = 0; i < perNotificationFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = perNotificationFilterFactories[i].CreateInstance(self, notification);
                    }
                }

                // Call PreHandleFilters

                if (preFilterItems.Count > 0)
                {
                    var context = new PreNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification
                    };
                    foreach (var filterAccessor in preFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            var preFilter = filter as IPreNotificationFilter;
                            if (preFilter != null)
                                preFilter.OnPreNotification(context);
                            else
                                await ((IPreNotificationAsyncFilter)filter).OnPreNotificationAsync(context);
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
                        await handler(self, notification.InvokePayload);
                    }
                    catch (Exception e)
                    {
                        // TODO: Exception Handling
                    }
                }

                // Call PostHandleFilters

                if (postFilterItems.Count > 0)
                {
                    var context = new PostNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification,
                    };
                    foreach (var filterAccessor in postFilterAccessors)
                    {
                        try
                        {
                            var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                            var postFilter = filter as IPostNotificationFilter;
                            if (postFilter != null)
                                postFilter.OnPostNotification(context);
                            else
                                await ((IPostNotificationAsyncFilter)filter).OnPostNotificationAsync(context);
                        }
                        catch (Exception e)
                        {
                            // TODO: what if exception thrown ?
                            Console.WriteLine(e);
                        }
                    }
                }
            };
        }

        private static Type[] GetInterfacePayloadTypeTable(Type interfaceType)
        {
            var payloadTableType =
                interfaceType.Assembly.GetTypes()
                             .Where(t =>
                             {
                                 var attr = t.GetCustomAttribute<PayloadTableAttribute>();
                                 return (attr != null && attr.Type == interfaceType && attr.Kind == PayloadTableKind.Notification);
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

            var payloadTypes = (Type[])queryMethodInfo.Invoke(null, new object[] { });
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
