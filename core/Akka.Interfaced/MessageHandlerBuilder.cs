using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Akka.Interfaced
{
    internal class MessageHandlerBuilder<T>
        where T : class
    {
        private Dictionary<Type, MessageHandlerItem<T>> _table;
        private FilterHandlerBuilder _filterHandlerBuilder;

        internal Dictionary<Type, MessageHandlerItem<T>> Build(FilterHandlerBuilder filterHandlerBuilder)
        {
            _table = new Dictionary<Type, MessageHandlerItem<T>>();
            _filterHandlerBuilder = filterHandlerBuilder;

            BuildAnnotatedMessageHandlers();

            return _table;
        }

        private void BuildAnnotatedMessageHandlers()
        {
            var type = typeof(T);

            // create a handler for every method which has MessageHandlerAttribute

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MessageHandlerAttribute>();
                if (attr == null)
                    continue;

                var messageType = attr.Type ?? method.GetParameters()[0].ParameterType;
                var isAsyncMethod = (method.ReturnType.Name.StartsWith("Task"));
                var filterChain = _filterHandlerBuilder.Build(method, FilterChainKind.Message);

                if (isAsyncMethod || filterChain.AsyncFilterExists)
                {
                    var item = new MessageHandlerItem<T>
                    {
                        IsReentrant = HandlerBuilderHelpers.IsReentrantMethod(method),
                        AsyncHandler = BuildAsyncHandler(messageType, method, filterChain)
                    };
                    _table.Add(messageType, item);
                }
                else
                {
                    if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                        throw new InvalidOperationException($"Async void handler is not supported. ({type.FullName}.{method.Name})");

                    var item = new MessageHandlerItem<T>
                    {
                        Handler = BuildHandler(messageType, method, filterChain)
                    };
                    _table.Add(messageType, item);
                }
            }
        }

        private static MessageHandler<T> BuildHandler(
            Type messageType, MethodInfo method, FilterChain filterChain)
        {
            var handler = MessageHandlerFuncBuilder.Build<T>(method);
            if (filterChain.Empty)
                return handler;

            return (self, message) =>
            {
                var filterPerInstanceProvider = filterChain.PerInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create PerRequest filters

                IFilter[] filterPerRequests = null;

                if (filterChain.PerInvokeFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[filterChain.PerInvokeFilterFactories.Length];
                    for (var i = 0; i < filterChain.PerInvokeFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = filterChain.PerInvokeFilterFactories[i].CreateInstance(self, message);
                    }
                }

                // Call PreFilters

                if (filterChain.PreFilterAccessors.Length > 0)
                {
                    var context = new PreMessageFilterContext
                    {
                        Actor = self,
                        Message = message
                    };
                    foreach (var filterAccessor in filterChain.PreFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        ((IPreMessageFilter)filter).OnPreMessage(context);
                    }
                }

                // Call Handler

                handler(self, message);

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostMessageFilterContext
                    {
                        Actor = self,
                        Message = message
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        ((IPostMessageFilter)filter).OnPostMessage(context);
                    }
                }
            };
        }

        private static MessageAsyncHandler<T> BuildAsyncHandler(
            Type messageType, MethodInfo method, FilterChain filterChain)
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? MessageHandlerAsyncBuilder.Build<T>(method)
                : MessageHandlerSyncToAsyncBuilder.Build<T>(method);
            if (filterChain.Empty)
                return handler;

            return async (self, message) =>
            {
                var filterPerInstanceProvider = filterChain.PerInstanceFilterExists ? (IFilterPerInstanceProvider)self : null;

                // Create PerRequest filters

                IFilter[] filterPerRequests = null;
                if (filterChain.PerInvokeFilterFactories.Length > 0)
                {
                    filterPerRequests = new IFilter[filterChain.PerInvokeFilterFactories.Length];
                    for (var i = 0; i < filterChain.PerInvokeFilterFactories.Length; i++)
                    {
                        filterPerRequests[i] = filterChain.PerInvokeFilterFactories[i].CreateInstance(self, message);
                    }
                }

                // Call PreFilters

                if (filterChain.PreFilterAccessors.Length > 0)
                {
                    var context = new PreMessageFilterContext
                    {
                        Actor = self,
                        Message = message
                    };
                    foreach (var filterAccessor in filterChain.PreFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        var preFilter = filter as IPreMessageFilter;
                        if (preFilter != null)
                            preFilter.OnPreMessage(context);
                        else
                            await ((IPreMessageAsyncFilter)filter).OnPreMessageAsync(context);
                    }
                }

                // Call Handler

                await handler(self, message);

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostMessageFilterContext
                    {
                        Actor = self,
                        Message = message,
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        var postFilter = filter as IPostMessageFilter;
                        if (postFilter != null)
                            postFilter.OnPostMessage(context);
                        else
                            await ((IPostMessageAsyncFilter)filter).OnPostMessageAsync(context);
                    }
                }
            };
        }
    }
}
