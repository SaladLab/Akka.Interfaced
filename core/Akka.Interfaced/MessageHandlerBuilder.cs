﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Akka.Interfaced
{
    internal class MessageHandlerBuilder
    {
        private Type _type;
        private FilterHandlerBuilder _filterHandlerBuilder;
        private Dictionary<Type, MessageHandlerItem> _table;

        internal Dictionary<Type, MessageHandlerItem> Build(Type type, FilterHandlerBuilder filterHandlerBuilder)
        {
            _type = type;
            _filterHandlerBuilder = filterHandlerBuilder;
            _table = new Dictionary<Type, MessageHandlerItem>();

            BuildAnnotatedMessageHandlers();

            return _table;
        }

        private void BuildAnnotatedMessageHandlers()
        {
            // create a handler for every method which has MessageHandlerAttribute

            var methods = _type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                    var item = new MessageHandlerItem
                    {
                        IsReentrant = HandlerBuilderHelpers.IsReentrantMethod(method),
                        AsyncHandler = BuildAsyncHandler(_type, messageType, method, filterChain)
                    };
                    _table.Add(messageType, item);
                }
                else
                {
                    if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                        throw new InvalidOperationException($"Async void handler is not supported. ({_type.FullName}.{method.Name})");

                    var item = new MessageHandlerItem
                    {
                        Handler = BuildHandler(_type, messageType, method, filterChain)
                    };
                    _table.Add(messageType, item);
                }
            }
        }

        private static MessageHandler BuildHandler(
            Type targetType, Type messageType, MethodInfo method, FilterChain filterChain)
        {
            var handler = MessageHandlerFuncBuilder.Build(targetType, method);
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

                var handled = false;
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
                    handled = context.Handled;
                }

                // Call Handler

                if (handled == false)
                    handler(self, message);

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostMessageFilterContext
                    {
                        Actor = self,
                        Message = message,
                        Intercepted = handled
                    };
                    foreach (var filterAccessor in filterChain.PostFilterAccessors)
                    {
                        var filter = filterAccessor(filterPerInstanceProvider, filterPerRequests);
                        ((IPostMessageFilter)filter).OnPostMessage(context);
                    }
                }
            };
        }

        private static MessageAsyncHandler BuildAsyncHandler(
            Type targetType, Type messageType, MethodInfo method, FilterChain filterChain)
        {
            var isAsyncMethod = method.ReturnType.Name.StartsWith("Task");
            var handler = isAsyncMethod
                ? MessageHandlerAsyncBuilder.Build(targetType, method)
                : MessageHandlerSyncToAsyncBuilder.Build(targetType, method);
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

                var handled = false;
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
                    handled = context.Handled;
                }

                // Call Handler

                if (handled == false)
                    await handler(self, message);

                // Call PostFilters

                if (filterChain.PostFilterAccessors.Length > 0)
                {
                    var context = new PostMessageFilterContext
                    {
                        Actor = self,
                        Message = message,
                        Intercepted = handled
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
