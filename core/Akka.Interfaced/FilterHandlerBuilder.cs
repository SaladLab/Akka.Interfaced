using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced
{
    internal class FilterItem
    {
        public FilterItem(FilterChainKind kind, IFilterFactory factory, IFilter referenceFilter, FilterAccessor accessor, int perInvokeIndex = -1)
        {
            Kind = kind;
            Factory = factory;
            ReferenceFilter = referenceFilter;
            Accessor = accessor;
            PerInvokeIndex = perInvokeIndex;
        }

        public FilterChainKind Kind { get; }
        public IFilterFactory Factory { get; }
        public IFilter ReferenceFilter { get; }
        public FilterAccessor Accessor { get; }
        public int PerInvokeIndex { get; }

        public int Order => ReferenceFilter.Order;

        public bool IsAsync =>
            (Kind == FilterChainKind.Request && (ReferenceFilter is IPreRequestAsyncFilter || ReferenceFilter is IPostRequestAsyncFilter)) ||
            (Kind == FilterChainKind.Notification && (ReferenceFilter is IPreNotificationAsyncFilter || ReferenceFilter is IPostNotificationAsyncFilter)) ||
            (Kind == FilterChainKind.Message && (ReferenceFilter is IPreMessageAsyncFilter || ReferenceFilter is IPostMessageAsyncFilter));

        public bool IsPreFilter =>
            (Kind == FilterChainKind.Request && (ReferenceFilter is IPreRequestFilter || ReferenceFilter is IPreRequestAsyncFilter)) ||
            (Kind == FilterChainKind.Notification && (ReferenceFilter is IPreNotificationFilter || ReferenceFilter is IPreNotificationAsyncFilter)) ||
            (Kind == FilterChainKind.Message && (ReferenceFilter is IPreMessageFilter || ReferenceFilter is IPreMessageAsyncFilter));

        public bool IsPostFilter =>
            (Kind == FilterChainKind.Request && (ReferenceFilter is IPostRequestFilter || ReferenceFilter is IPostRequestAsyncFilter)) ||
            (Kind == FilterChainKind.Notification && (ReferenceFilter is IPostNotificationFilter || ReferenceFilter is IPostNotificationAsyncFilter)) ||
            (Kind == FilterChainKind.Message && (ReferenceFilter is IPostMessageFilter || ReferenceFilter is IPostMessageAsyncFilter));

        public bool IsPerInstance =>
            Factory is IFilterPerInstanceFactory ||
            Factory is IFilterPerInstanceMethodFactory;

        public bool IsPerInvoke => Factory is IFilterPerInvokeFactory;
    }

    internal class FilterChain
    {
        public FilterAccessor[] PreFilterAccessors;
        public FilterAccessor[] PostFilterAccessors;
        public IFilterPerInvokeFactory[] PerInvokeFilterFactories;
        public bool Empty;
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
                Empty = filterItems.Any() == false,
                AsyncFilterExists = filterItems.Any(f => f.IsAsync),
                PerInstanceFilterExists = filterItems.Any(f => f.IsPerInstance),
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
                            filterItem = new FilterItem(kind, filterFactory, filter, (_, x) => filter);
                            _perClassFilterItemTable.Add(factoryType, filterItem);
                        }
                    }
                    else if (filterItem.Kind != kind)
                    {
                        filterItem = null;
                    }
                }
                else if (filterFactory is IFilterPerClassMethodFactory)
                {
                    var factory = (IFilterPerClassMethodFactory)filterFactory;
                    factory.Setup(type, method);
                    var filter = factory.CreateInstance();
                    if (CheckFilterKind(filter, kind))
                    {
                        filterItem = new FilterItem(kind, filterFactory, filter, (_, x) => filter);
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
                            filterItem = new FilterItem(kind, filterFactory, filter,
                                                        (provider, _) => provider.GetFilter(arrayIndex));
                            _perInstanceFilterItemTable.Add(factoryType, filterItem);
                        }
                    }
                    else if (filterItem.Kind != kind)
                    {
                        filterItem = null;
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
                        filterItem = new FilterItem(kind, filterFactory, filter,
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
                        filterItem = new FilterItem(kind, filterFactory, filter,
                                                    (_, filters) => filters[arrayIndex], filterPerInvokeIndex);
                    }
                }

                if (filterItem != null)
                    filterItems.Add(filterItem);
            }

            return filterItems;
        }

        private static bool CheckFilterKind(IFilter filter, FilterChainKind kind)
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
