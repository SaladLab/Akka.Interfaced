using System;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced.LogFilter
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class LogAttribute : Attribute, IFilterPerClassMethodFactory
    {
        private readonly string _loggerName;
        private readonly string _logLevel;
        private readonly int _filterOrder;
        private ILogProxy _logProxy;
        private MethodInfo _method;

        public LogAttribute(string loggerName = null, string logLevel = "Trace", int filterOrder = 0)
        {
            _loggerName = loggerName;
            _logLevel = logLevel;
            _filterOrder = filterOrder;
        }

        void IFilterPerClassMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _logProxy = LogProxyBuilder.Build(actorType, _loggerName, _logLevel);
            _method = method;
        }

        IFilter IFilterPerClassMethodFactory.CreateInstance()
        {
            return new LogFilter(_filterOrder, _logProxy, _method);
        }
    }
}
