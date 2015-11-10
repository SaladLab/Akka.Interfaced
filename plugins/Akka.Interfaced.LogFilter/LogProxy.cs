using System;

namespace Akka.Interfaced.LogFilter
{
    internal interface ILogProxy
    {
        bool IsEnabled(object actor);
        void Log(object actor, string message);
    }

    internal class LogProxy : ILogProxy
    {
        private readonly Template _template;

        public class Template
        {
            public Func<object, bool> IsEnabledMethod;
            public Action<object, string> LogMethod;
        }

        public LogProxy(Template template)
        {
            _template = template;
        }

        public bool IsEnabled(object actor)
        {
            return _template.IsEnabledMethod(actor);
        }

        public void Log(object actor, string message)
        {
            _template.LogMethod(actor, message);
        }
    }
}
