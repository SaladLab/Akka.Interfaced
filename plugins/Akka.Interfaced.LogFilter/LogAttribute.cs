using System;
using System.Reflection;

namespace Akka.Interfaced.LogFilter
{
    [Flags]
    public enum LogFilterTarget
    {
        None = 0,
        Request = 1,
        Notification = 2,
        Message = 4,
        All = Request | Notification | Message
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class LogAttribute : Attribute, IFilterPerClassMethodFactory
    {
        private readonly LogFilterTarget _target;
        private readonly int _filterOrder;
        private readonly string _loggerName;
        private readonly string _logLevel;
        private ILogProxy _logProxy;
        private MethodInfo _method;

        public LogAttribute(LogFilterTarget target = LogFilterTarget.All, int filterOrder = 0, string loggerName = null, string logLevel = "Trace")
        {
            _target = target;
            _filterOrder = filterOrder;
            _loggerName = loggerName;
            _logLevel = logLevel;
        }

        void IFilterPerClassMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _logProxy = LogProxyBuilder.Build(actorType, _loggerName, _logLevel);
            _method = method;
        }

        IFilter IFilterPerClassMethodFactory.CreateInstance()
        {
            return new LogFilter(_target, _filterOrder, _logProxy, _method);
        }
    }
}
