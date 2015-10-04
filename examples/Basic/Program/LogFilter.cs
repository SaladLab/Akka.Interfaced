using System;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;
using Newtonsoft.Json;

namespace Basic.Program
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LogAttribute : Attribute, IFilterPerClassMethodFactory
    {
        IFilter IFilterPerClassMethodFactory.CreateInstance(Type actorType, MethodInfo method)
        {
            return new LogFilter(actorType, method);
        }
    }

    public sealed class LogFilter : IPreHandleFilter, IPostHandleFilter
    {
        private string _methodShortName;

        public LogFilter(Type actorType, MethodInfo method)
        {
            _methodShortName = actorType.Name + "." + method.Name.Split('.').Last();
        }

        int IFilter.Order
        {
            get
            {
                return 0;
            }
        }

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            var invokeJson = JsonConvert.SerializeObject(context.Request.InvokePayload, Formatting.None);
            Console.WriteLine("#{0} -> {1} {2}",
                              context.Request.RequestId, _methodShortName, invokeJson);
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            if (context.Response.Exception != null)
            {
                Console.WriteLine("#{0} <- {1} Exception: {2}",
                                  context.Request.RequestId, _methodShortName, context.Response.Exception);
            }
            else if (context.Response.ReturnPayload != null)
            {
                var returnJson = JsonConvert.SerializeObject(context.Response.ReturnPayload, Formatting.None);
                Console.WriteLine("#{0} <- {1} {2}",
                                  context.Request.RequestId, _methodShortName, returnJson);
            }
            else
            {
                Console.WriteLine("#{0} <- {1} <void>",
                                  context.Request.RequestId, _methodShortName);
            }
        }
    }
}
