using System;
using System.Linq;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ResponsiveException
        : Attribute, IFilterPerClassFactory, IPostRequestFilter
    {
        private Func<Exception, bool> _filter;

        int IFilter.Order => 0;

        public ResponsiveException(Func<Exception, bool> filter)
        {
            _filter = filter;
        }

        public ResponsiveException(params Type[] exceptionTypes)
        {
            _filter = e => exceptionTypes.Any(t => e.GetType() == t);
        }

        void IFilterPerClassFactory.Setup(Type actorType)
        {
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return this;
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
}
