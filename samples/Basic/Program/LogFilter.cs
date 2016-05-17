using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;
using Newtonsoft.Json;

namespace Basic.Program
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class LogAttribute : Attribute, IFilterPerInvokeFactory
    {
        private string _methodName;

        void IFilterPerInvokeFactory.Setup(Type actorType, MethodInfo method)
        {
            _methodName = actorType.Name + "." + method.Name.Split('.').Last();
        }

        IFilter IFilterPerInvokeFactory.CreateInstance(object actor, RequestMessage request)
        {
            return new LogFilter(_methodName, actor, request);
        }
    }

    public sealed class LogFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _methodName;
        private Stopwatch _watch;

        public LogFilter(string methodName, object actor, RequestMessage request)
        {
            _methodName = methodName;
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            _watch = new Stopwatch();
            _watch.Start();

            var invokeJson = JsonConvert.SerializeObject(context.Request.InvokePayload, Formatting.None);
            Console.WriteLine("#{0} -> {1} {2}",
                              context.Request.RequestId, _methodName, invokeJson);
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            _watch.Stop();
            var elapsed = _watch.ElapsedMilliseconds;

            if (context.Response.Exception != null)
            {
                Console.WriteLine("#{0} <- {1} Exception: {2} ({3}ms)",
                                  context.Request.RequestId, _methodName, context.Response.Exception, elapsed);
            }
            else if (context.Response.ReturnPayload != null)
            {
                var returnJson = JsonConvert.SerializeObject(context.Response.ReturnPayload, Formatting.None);
                Console.WriteLine("#{0} <- {1} {2} ({3}ms)",
                                  context.Request.RequestId, _methodName, returnJson, elapsed);
            }
            else
            {
                Console.WriteLine("#{0} <- {1} <void> ({2}ms)",
                                  context.Request.RequestId, _methodName, elapsed);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class SimpleLogAttribute : Attribute, IFilterPerClassFactory
    {
        private string _typeName;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _typeName = actorType.Name;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new SimpleLogFilter(_typeName);
        }
    }

    public sealed class SimpleLogFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _typeName;
        private int _handleCount;

        public SimpleLogFilter(string typeName)
        {
            _typeName = typeName;
        }

        int IFilter.Order => -1;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            _handleCount += 1;
            Console.WriteLine("@{0} : OnPreHandle #{1}", _typeName, _handleCount);
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            Console.WriteLine("@{0} : OnPostHandle", _typeName);
        }
    }
}
