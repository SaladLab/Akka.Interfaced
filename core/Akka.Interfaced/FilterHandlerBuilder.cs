using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Akka.Interfaced
{
    internal class FilterItem
    {
        public FilterItem(IFilterFactory factory, IFilter referenceFilter, FilterAccessor accessor, int perInvokeIndex = -1)
        {
            Factory = factory;
            Accessor = accessor;

            Order = referenceFilter.Order;

            IsAsync = referenceFilter is IPreRequestAsyncFilter || referenceFilter is IPostRequestAsyncFilter ||
                      referenceFilter is IPreNotificationAsyncFilter || referenceFilter is IPostNotificationAsyncFilter ||
                      referenceFilter is IPreMessageAsyncFilter || referenceFilter is IPostMessageAsyncFilter;

            IsPreFilter = referenceFilter is IPreRequestFilter || referenceFilter is IPreRequestAsyncFilter ||
                          referenceFilter is IPreNotificationFilter || referenceFilter is IPreNotificationAsyncFilter ||
                          referenceFilter is IPreMessageFilter || referenceFilter is IPreMessageAsyncFilter;

            IsPostFilter = referenceFilter is IPostRequestFilter || referenceFilter is IPostRequestAsyncFilter ||
                           referenceFilter is IPostNotificationFilter || referenceFilter is IPostNotificationAsyncFilter ||
                           referenceFilter is IPostMessageFilter || referenceFilter is IPostMessageAsyncFilter;

            IsPerInstance = factory is IFilterPerInstanceFactory ||
                            factory is IFilterPerInstanceMethodFactory;

            IsPerInvoke = factory is IFilterPerInvokeFactory;

            PerInvokeIndex = perInvokeIndex;
        }

        public IFilterFactory Factory { get; }
        public FilterAccessor Accessor { get; }
        public int Order { get; }
        public bool IsAsync { get; }
        public bool IsPreFilter { get; }
        public bool IsPostFilter { get; }
        public bool IsPerInstance { get; }
        public bool IsPerInvoke { get; }
        public int PerInvokeIndex { get; }
    }

    internal class FilterChain
    {
        public FilterAccessor[] PreFilterAccessors;
        public FilterAccessor[] PostFilterAccessors;
        public IFilterPerInvokeFactory[] PerInvokeFilterFactories;
        public bool AsyncFilterExists;
        public bool PerInstanceFilterExists;
    }

    internal enum FilterChainKind
    {
        Request = 1,
        Notification = 2,
        Message = 3,
    }

    internal class FilterHandlerBuilder
    {
        private Type _type;
        private Dictionary<Type, FilterItem> _perClassFilterItemTable = new Dictionary<Type, FilterItem>();
        private Dictionary<Type, FilterItem> _perInstanceFilterItemTable = new Dictionary<Type, FilterItem>();
        private List<Func<object, IFilter>> _perInstanceFilterCreators = new List<Func<object, IFilter>>();

        public List<Func<object, IFilter>> PerInstanceFilterCreators => _perInstanceFilterCreators;

        public FilterHandlerBuilder(Type type)
        {
            _type = type;
        }

        public FilterChain Build(MethodInfo method, FilterChainKind kind)
        {
            var filterItems = BuildFilterItems(_type, method, kind).OrderBy(f => f.Order).ToList();

            return new FilterChain
            {
                PreFilterAccessors = filterItems.Where(f => f.IsPreFilter).Select(f => f.Accessor).ToArray(),
                PostFilterAccessors = filterItems.Where(f => f.IsPostFilter).Select(f => f.Accessor).Reverse().ToArray(),
                PerInvokeFilterFactories = filterItems.Where(f => f.IsPerInvoke).GroupBy(f => f.PerInvokeIndex)
                                                      .OrderBy(g => g.Key).Select(g => (IFilterPerInvokeFactory)g.Last().Factory).ToArray(),
                PerInstanceFilterExists = filterItems.Any(f => f.IsPerInstance),
                AsyncFilterExists = filterItems.Any(f => f.IsAsync)
            };
        }

        private List<FilterItem> BuildFilterItems(Type type, MethodInfo method, FilterChainKind kind)
        {
            var filterItems = new List<FilterItem>();
            var filterPerInvokeIndex = 0;

            // scan all filters for this method and construct a list of filter item
            // discard filters that doesn't provide a exact filter (such as a filter doesn't have OnPreRequest on kind=Request)

            var filterFactories = type.GetCustomAttributes().Concat(method.GetCustomAttributes()).OfType<IFilterFactory>();
            foreach (var filterFactory in filterFactories)
            {
                FilterItem filterItem = null;

                if (filterFactory is IFilterPerClassFactory)
                {
                    var factoryType = filterFactory.GetType();
                    if (_perClassFilterItemTable.TryGetValue(factoryType, out filterItem) == false)
                    {
                        var factory = (IFilterPerClassFactory)filterFactory;
                        factory.Setup(type);
                        var filter = factory.CreateInstance();
                        if (CheckFilterKind(filter, kind))
                        {
                            filterItem = new FilterItem(filterFactory, filter, (_, x) => filter);
                            _perClassFilterItemTable.Add(factoryType, filterItem);
                        }
                    }
                }
                else if (filterFactory is IFilterPerClassMethodFactory)
                {
                    var factory = (IFilterPerClassMethodFactory)filterFactory;
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance();
                    if (CheckFilterKind(filter, kind))
                    {
                        filterItem = new FilterItem(filterFactory, filter, (_, x) => filter);
                    }
                }
                else if (filterFactory is IFilterPerInstanceFactory)
                {
                    var factoryType = filterFactory.GetType();
                    if (_perInstanceFilterItemTable.TryGetValue(factoryType, out filterItem) == false)
                    {
                        var factory = ((IFilterPerInstanceFactory)filterFactory);
                        factory.Setup(type);
                        var filter = factory.CreateInstance(null);
                        if (CheckFilterKind(filter, kind))
                        {
                            var arrayIndex = _perInstanceFilterCreators.Count;
                            _perInstanceFilterCreators.Add(a => factory.CreateInstance(a));
                            filterItem = new FilterItem(filterFactory, filter,
                                                        (provider, _) => provider.GetFilter(arrayIndex));
                            _perInstanceFilterItemTable.Add(factoryType, filterItem);
                        }
                    }
                }
                else if (filterFactory is IFilterPerInstanceMethodFactory)
                {
                    var factory = ((IFilterPerInstanceMethodFactory)filterFactory);
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance(null);
                    if (CheckFilterKind(filter, kind))
                    {
                        var arrayIndex = _perInstanceFilterCreators.Count;
                        _perInstanceFilterCreators.Add(a => factory.CreateInstance(a));
                        filterItem = new FilterItem(filterFactory, filter,
                                                    (provider, _) => provider.GetFilter(arrayIndex));
                    }
                }
                else if (filterFactory is IFilterPerInvokeFactory)
                {
                    var factory = ((IFilterPerInvokeFactory)filterFactory);
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance(null, null);
                    if (CheckFilterKind(filter, kind))
                    {
                        var arrayIndex = filterPerInvokeIndex++;
                        filterItem = new FilterItem(filterFactory, filter,
                                                    (_, filters) => filters[arrayIndex], filterPerInvokeIndex);
                    }
                }

                if (filterItem != null)
                    filterItems.Add(filterItem);
            }

            return filterItems;
        }

        private bool CheckFilterKind(IFilter filter, FilterChainKind kind)
        {
            switch (kind)
            {
                case FilterChainKind.Request:
                    return filter is IPreRequestFilter || filter is IPreRequestAsyncFilter ||
                           filter is IPostRequestFilter || filter is IPostRequestAsyncFilter;

                case FilterChainKind.Notification:
                    return filter is IPreNotificationFilter || filter is IPreNotificationAsyncFilter ||
                           filter is IPostNotificationFilter || filter is IPostNotificationAsyncFilter;

                case FilterChainKind.Message:
                    return filter is IPreMessageFilter || filter is IPreMessageAsyncFilter ||
                           filter is IPostMessageFilter || filter is IPostMessageAsyncFilter;

                default:
                    return false;
            }
        }
    }
}
