using System;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced.LogFilter
{
    internal static class LogProxyBuilder
    {
        public static LogProxy Build(Type targetType, string loggerName, string logLevel)
        {
            var logger = GetLoggerAccessor(targetType, loggerName);
            var template = GetLogProxyTemplate(logger.Item1, logger.Item2, logLevel);
            return new LogProxy(template);
        }

        private static Tuple<Type, Func<object, object>> GetLoggerAccessor(Type targetType, string loggerName)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (string.IsNullOrEmpty(loggerName))
            {
                // *Logger* field
                var field = targetType.GetFields(bindingFlags).FirstOrDefault(f => f.Name.ToLower().Contains("logger"));
                if (field != null)
                    return Tuple.Create<Type, Func<object, object>>(field.FieldType, o => field.GetValue(o));

                // *Logger* property
                var property = targetType.GetProperties(bindingFlags).FirstOrDefault(f => f.Name.ToLower().Contains("logger"));
                if (property != null)
                    return Tuple.Create<Type, Func<object, object>>(property.PropertyType, o => property.GetValue(o));

                // *Log* field
                var field2 = targetType.GetFields(bindingFlags).FirstOrDefault(f => f.Name.ToLower().Contains("log"));
                if (field2 != null)
                    return Tuple.Create<Type, Func<object, object>>(field2.FieldType, o => field2.GetValue(o));

                // *Log* property
                var property2 = targetType.GetProperties(bindingFlags).FirstOrDefault(f => f.Name.ToLower().Contains("log"));
                if (property2 != null)
                    return Tuple.Create<Type, Func<object, object>>(property2.PropertyType, o => property2.GetValue(o));
            }
            else
            {
                var field = targetType.GetField(loggerName, bindingFlags);
                if (field != null)
                    return Tuple.Create<Type, Func<object, object>>(field.FieldType, o => field.GetValue(o));

                var property = targetType.GetProperty(loggerName, bindingFlags);
                if (property != null)
                    return Tuple.Create<Type, Func<object, object>>(property.PropertyType, o => property.GetValue(o));
            }

            return null;
        }

        private static LogProxy.Template GetLogProxyTemplate(Type loggerType, Func<object, object> loggerAccessor, string logLevel)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var isEnabledPi = loggerType.GetProperties(bindingFlags).FirstOrDefault(
                p => p.Name == $"Is{logLevel}Enabled");

            if (isEnabledPi == null)
                throw new InvalidOperationException(
                    $"Cannot find Is{logLevel}Enabled property from {loggerType.FullName}");

            var logMi = loggerType.GetMethods(bindingFlags).FirstOrDefault(
                m => m.Name == logLevel &&
                     m.GetParameters().Length == 1 &&
                     m.GetParameters()[0].ParameterType == typeof(string));

            if (logMi == null)
                throw new InvalidOperationException(
                    $"Cannot find {logLevel} method from {loggerType.FullName}");

            return new LogProxy.Template
            {
                IsEnabledMethod = actor => (bool)isEnabledPi.GetValue(loggerAccessor(actor)),
                LogMethod = (actor, message) => logMi.Invoke(loggerAccessor(actor), new object[] { message })
            };
        }
    }
}
