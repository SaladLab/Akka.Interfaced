using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CodeGen
{
    public static class Utility
    {
        public static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericParams = String.Join(", ", type.GenericTypeArguments.Select(t => GetTypeName(t)));
                var delimiterPos = type.Name.IndexOf('`');
                return type.Namespace + "." + type.Name.Substring(0, delimiterPos) + "<" + genericParams + ">";
            }
            else
            {
                return type.FullName;
            }
        }

        public static string GetTransportTypeName(Type type)
        {
            if (IsActorInterface(type))
                return type.Namespace + "." + GetActorRefClassName(type);
            else if (IsObserverInterface(type))
                return type.Namespace + "." + GetObserverClassName(type);
            else
                return GetTypeName(type);
        }

        // return "(CounterRef)" when type is ICounter
        // but return "" when type is CounterRef
        public static string GetTransportTypeCasting(Type type)
        {
            var typeName = type.FullName;
            var transportTypeName = Utility.GetTransportTypeName(type);

            return typeName == transportTypeName
                ? ""
                : string.Format("({0})", transportTypeName);
        }

        public static bool IsActorInterface(Type type)
        {
            return type.IsInterface &&
                   type.GetInterfaces().Any(i => i.FullName == "Akka.Interfaced.IInterfacedActor");
        }

        public static bool IsObserverInterface(Type type)
        {
            return type.IsInterface &&
                   type.GetInterfaces().Any(i => i.FullName == "Akka.Interfaced.IInterfacedObserver");
        }

        public static string GetNoReplyInterfaceName(Type type)
        {
            return type.Name + "_NoReply";
        }

        public static string GetActorRefClassName(Type type)
        {
            return type.Name.Substring(1) + "Ref";
        }

        public static string GetObserverClassName(Type type)
        {
            return type.Name.Substring(1);
        }

        public static string GetActorInterfaceTagName(Type type)
        {
            var attr = type.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().FullName == "Akka.Interfaced.TagOverridableAttribute");
            if (attr == null)
                return null;

            var pi = attr.GetType().GetProperty("Name");
            if (pi == null)
                return null;

            return (string)pi.GetValue(attr);
        }

        public static string GetParameterDeclaration(ParameterInfo pi, bool includeDefaultExpression)
        {
            var defaultValue = pi.HasDefaultValue ? GetValueLiteral(pi.DefaultValue) : "";
            return (pi.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") +
                   (Utility.GetTypeName(pi.ParameterType) + " " + pi.Name) +
                   (includeDefaultExpression && defaultValue != "" ? " = " + defaultValue : "");
        }

        public static bool HasNonTrivialDefaultValue(this ParameterInfo pi)
        {
            if (pi.HasDefaultValue == false || pi.DefaultValue == null)
                return false;

            if (pi.DefaultValue.GetType().IsValueType == false)
                return false;

            return pi.DefaultValue.Equals(Activator.CreateInstance(pi.ParameterType)) == false;
        }

        public static string GetValueLiteral(object value)
        {
            // NOTE: For simple implementation, escaping string is not considered.

            if (value == null)
                return "null";

            var type = value.GetType();

            if (type == typeof(string))
                return string.Format("\"{0}\"", value);

            if (type == typeof(char))
                return string.Format("'{0}'", value);

            return string.Format("{0}", value);
        }
    }
}
