using System;
using System.Linq;

namespace Akka.Interfaced
{
    public class ResponsiveExceptionFilter : IPostRequestFilter
    {
        private int _filterOrder;
        private Func<Exception, bool> _filter;

        int IFilter.Order => _filterOrder;

        public ResponsiveExceptionFilter(int filterOrder, Func<Exception, bool> filter)
        {
            _filterOrder = filterOrder;
            _filter = filter;
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            if (context.Exception != null && _filter(context.Exception))
            {
                context.Response = new ResponseMessage
                {
                    RequestId = context.Request.RequestId,
                    Exception = context.Exception
                };
                context.Exception = null;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ResponsiveExceptionAttribute : Attribute, IFilterPerClassFactory
    {
        public const int FilterDefaultOrder = 10;

        private int _filterOrder;
        private Func<Exception, bool> _filter;

        public ResponsiveExceptionAttribute(int filterOrder = FilterDefaultOrder, Func<Exception, bool> filter = null)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _filterOrder = filterOrder;
            _filter = filter;
        }

        public ResponsiveExceptionAttribute(params Type[] exceptionTypes)
            : this(FilterDefaultOrder, exceptionTypes)
        {
        }

        public ResponsiveExceptionAttribute(int filterOrder, params Type[] exceptionTypes)
        {
            _filterOrder = filterOrder;
            _filter = e => exceptionTypes.Any(t => e.GetType() == t);
        }

        void IFilterPerClassFactory.Setup(Type actorType)
        {
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new ResponsiveExceptionFilter(_filterOrder, _filter);
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ResponsiveExceptionAllAttribute : ResponsiveExceptionAttribute
    {
        public ResponsiveExceptionAllAttribute(int filterOrder = FilterDefaultOrder)
            : base(filterOrder, filter: _ => true)
        {
        }
    }
}
