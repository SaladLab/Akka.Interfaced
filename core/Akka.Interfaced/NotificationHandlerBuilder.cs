using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable SA1130 // Use lambda syntax

namespace Akka.Interfaced
{
    internal class NotificationHandlerBuilder
    {
        private Type _type;
        private FilterHandlerBuilder _filterHandlerBuilder;
        private Dictionary<Type, NotificationHandlerItem> _table;

        public Dictionary<Type, NotificationHandlerItem> Build(Type type, FilterHandlerBuilder filterHandlerBuilder)
        {
            _type = type;
            _filterHandlerBuilder = filterHandlerBuilder;
            _table = new Dictionary<Type, NotificationHandlerItem>();

            BuildRegularInterfaceHandlers();
            BuildExtendedInterfaceHandlers();

            return _table;
        }

        private void BuildRegularInterfaceHandlers()
        {
            foreach (var ifs in _type.GetInterfaces())
            {
                if (ifs.GetInterfaces().All(t => t != typeof(IInterfacedObserver)))
                    continue;

                var interfaceMap = _type.GetInterfaceMap(ifs);
                var methodItems = interfaceMap.InterfaceMethods.Zip(interfaceMap.TargetMethods, Tuple.Create)
                                              .OrderBy(p => p.Item1, new MethodInfoComparer())
                                              .ToArray();
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);

                // build a decorated handler for each method.

                for (var i = 0; i < methodItems.Length; i++)
                {
                    var targetMethod = methodItems[i].Item2;
                    var invokePayloadType = payloadTypeTable[i];
                    var filterChain = _filterHandlerBuilder.Build(targetMethod, FilterChainKind.Notification);

                    if (filterChain.AsyncFilterExists)
                    {
                        // async handler

                        _table.Add(invokePayloadType, new NotificationHandlerItem
                        {
                            InterfaceType = ifs,
                            IsReentrant = HandlerBuilderHelpers.IsReentrantMethod(targetMethod),
                            AsyncHandler = BuildAsyncHandler(_type, invokePayloadType, targetMethod, filterChain)
                        });
                    }
                    else
                    {
                        // sync handler

                        _table.Add(invokePayloadType, new NotificationHandlerItem
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = BuildHandler(_type, invokePayloadType, targetMethod, filterChain)
                        });
                    }
                }
            }
        }

        private void BuildExtendedInterfaceHandlers()
        {
            var targetMethods =
                _type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     .Where(m => m.GetCustomAttribute<ExtendedHandlerAttribute>() != null)
                     .Select(m => Tuple.Create(m, m.GetCustomAttribute<ExtendedHandlerAttribute>()))
                     .ToList();

            var extendedInterfaces =
                _type.GetInterfaces()
                     .Where(t => t.FullName.StartsWith("Akka.Interfaced.IExtendedInterface"))
                     .SelectMany(t => t.GenericTypeArguments)
                     .Where(t => t.GetInterfaces().Any(i => i == typeof(IInterfacedObserver)));

            foreach (var ifs in extendedInterfaces)
            {
                var payloadTypeTable = GetInterfacePayloadTypeTable(ifs);
                var interfaceMethods = ifs.GetMethods().OrderBy(m => m, new MethodInfoComparer()).ToArray();

                // build a decorated handler for each method.

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

                        if (HandlerBuilderHelpers.AreParameterTypesEqual(method.Item1.GetParameters(), parameters))
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
                    var filterChain = _filterHandlerBuilder.Build(targetMethod, FilterChainKind.Notification);

                    if (isAsyncMethod || filterChain.AsyncFilterExists)
                    {
                        // async handler

                        _table.Add(invokePayloadType, new NotificationHandlerItem
                        {
                            InterfaceType = ifs,
                            IsReentrant = HandlerBuilderHelpers.IsReentrantMethod(targetMethod),
                            AsyncHandler = BuildAsyncHandler(_type, invokePayloadType, targetMethod, filterChain)
                        });
                    }
                    else
                    {
                        if (targetMethod.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                            throw new InvalidOperationException($"Async void handler is not supported. ({_type.FullName}.{targetMethod.Name})");

                        // sync handler

                        _table.Add(invokePayloadType, new NotificationHandlerItem
                        {
                            InterfaceType = ifs,
                            IsReentrant = false,
                            Handler = BuildHandler(_type, invokePayloadType, targetMethod, filterChain)
                        });
                    }
                }
            }
        }

        private static NotificationHandler BuildHandler(
            Type targetType, Type invokePayloadType, MethodInfo method, FilterChain filterChain)
        {
            var handler = RequestHandlerFuncBuilder.Build(targetType, invokePayloadType, null, method);

            return delegate(object self, NotificationMessage notification)
            {
                var filterPerInstanceProvider = filterChain.PerInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create PerRequest filters

                IFilter[] filterPerRequests = null;

                if (filterChain.PerInvokeFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[filterChain.PerInvokeFilterFactories.Length];
                    for (var i = 0; i < filterChain.PerInvokeFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = filterChain.PerInvokeFilterFactories[i].CreateInstance(self, notification);
                    }
                }

                // Call PreFilters

                var handled = false;
                if (filterChain.PreFilterAccessors.Length > 0)
                {
                    var context = new PreNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification
                    };
                    foreach (var filterAccessor in filterChain.PreFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        ((IPreNotificationFilter)filter).OnPreNotification(context);
                    }
                    handled = context.Handled;
                }

                // Call Handler

                if (handled == false)
                    handler(self, notification.InvokePayload);

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification,
                        Intercepted = handled
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        ((IPostNotificationFilter)filter).OnPostNotification(context);
                    }
                }
            };
        }

        private static NotificationAsyncHandler BuildAsyncHandler(
            Type targetType, Type invokePayloadType, MethodInfo method, FilterChain filterChain)
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? RequestHandlerAsyncBuilder.Build(targetType, invokePayloadType, null, method)
                : RequestHandlerSyncToAsyncBuilder.Build(targetType, invokePayloadType, null, method);

            // TODO: Optimize this function when without async filter
            return async delegate(object self, NotificationMessage notification)
            {
                var filterPerInstanceProvider = filterChain.PerInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create PerRequest filters

                IFilter[] filterPerRequests = null;
                if (filterChain.PerInvokeFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[filterChain.PerInvokeFilterFactories.Length];
                    for (var i = 0; i < filterChain.PerInvokeFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = filterChain.PerInvokeFilterFactories[i].CreateInstance(self, notification);
                    }
                }

                // Call PreFilters

                var handled = false;
                if (filterChain.PreFilterAccessors.Length > 0)
                {
                    var context = new PreNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification
                    };
                    foreach (var filterAccessor in filterChain.PreFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        var preFilter = filter as IPreNotificationFilter;
                        if (preFilter != null)
                            preFilter.OnPreNotification(context);
                        else
                            await ((IPreNotificationAsyncFilter)filter).OnPreNotificationAsync(context);
                    }
                    handled = context.Handled;
                }

                // Call Handler

                if (handled == false)
                    await handler(self, notification.InvokePayload);

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostNotificationFilterContext
                    {
                        Actor = self,
                        Notification = notification,
                        Intercepted = handled
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        var postFilter = filter as IPostNotificationFilter;
                        if (postFilter != null)
                            postFilter.OnPostNotification(context);
                        else
                            await ((IPostNotificationAsyncFilter)filter).OnPostNotificationAsync(context);
                    }
                }
            };
        }

        private static Type[] GetInterfacePayloadTypeTable(Type interfaceType)
        {
            var payloadTypes = (Type[])HandlerBuilderHelpers.GetInterfacePayloadTypeTable(interfaceType, PayloadTableKind.Notification);
            if (payloadTypes == null || payloadTypes.GetLength(0) != interfaceType.GetMethods().Length)
            {
                throw new InvalidOperationException(
                    $"Mismatched a payload table for {interfaceType.FullName}");
            }

            return payloadTypes;
        }
    }
}
