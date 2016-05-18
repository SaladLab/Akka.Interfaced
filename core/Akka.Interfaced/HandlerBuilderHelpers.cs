using System;
using System.Linq;
using System.Reflection;

namespace Akka.Interfaced
{
    internal static class HandlerBuilderHelpers
    {
        public static bool IsReentrantMethod(MethodInfo method)
        {
            return method.CustomAttributes.Any(x => x.AttributeType == typeof(ReentrantAttribute));
        }

        public static bool AreParameterTypesEqual(ParameterInfo[] a, ParameterInfo[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].ParameterType != b[i].ParameterType)
                    return false;
            }
            return true;
        }

        public static object GetInterfacePayloadTypeTable(Type interfaceType, PayloadTableKind kind)
        {
            var payloadTableType =
                interfaceType.Assembly.GetTypes()
                             .Where(t =>
                             {
                                 var attr = t.GetCustomAttribute<PayloadTableAttribute>();
                                 return (attr != null && attr.Type == interfaceType && attr.Kind == kind);
                             })
                             .FirstOrDefault();

            if (payloadTableType == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find payload table class for {interfaceType.FullName}");
            }

            var queryMethodInfo = payloadTableType.GetMethod("GetPayloadTypes");
            if (queryMethodInfo == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find {payloadTableType.FullName}.GetPayloadTypes()");
            }

            var value = queryMethodInfo.Invoke(null, new object[] { });
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"Invalid null from {payloadTableType.FullName}.GetPayloadTypes()");
            }

            return value;
        }
    }
}
