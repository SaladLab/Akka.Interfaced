using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeGenerator
{
    public static class Utility
    {
        public static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericParams = string.Join(", ", type.GenericTypeArguments.Select(t => GetTypeName(t)));
                var delimiterPos = type.Name.IndexOf('`');
                return type.Namespace + "." + type.Name.Substring(0, delimiterPos) + "<" + genericParams + ">";
            }
            else
            {
                return type.FullName;
            }
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

        // Naming generated type guideline:
        // - if user will not type a type, use underscore to avoid a conflict with user types.
        //   (e.g. *_PayloadTable, *_NoReply)
        // - if user will type a type, no underscore.
        //   (e.g. *Ref, *Sync)

        public static string GetPayloadTableClassName(Type type)
        {
            return type.Name + "_PayloadTable";
        }

        public static string GetNoReplyInterfaceName(Type type)
        {
            return type.Name + "_NoReply";
        }

        public static string GetActorRefClassName(Type type)
        {
            // because user will type this type, no _ prefix.
            return type.Name.Substring(1) + "Ref";
        }

        public static string GetActorSyncInterfaceName(Type type)
        {
            // because user will type this type, no _ prefix.
            return type.Name + "Sync";
        }

        public static string GetObserverClassName(Type type)
        {
            return type.Name.Substring(1);
        }

        public static string GetObserverAsyncInterfaceName(Type type)
        {
            // because user will type this type, no _ prefix.
            return type.Name + "Async";
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

        public static string GetSurrogateClassName(Type type)
        {
            return GetSurrogateClassName(type.Name);
        }

        public static string GetSurrogateClassName(string typeName)
        {
            return "SurrogateFor" + typeName;
        }

        public static string GetParameterDeclaration(ParameterInfo pi, bool includeDefaultExpression)
        {
            var defaultValue = pi.HasDefaultValue ? GetValueLiteral(pi.DefaultValue) : "";
            return (pi.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") +
                   (Utility.GetTypeName(pi.ParameterType) + " " + pi.Name) +
                   (includeDefaultExpression && defaultValue != "" ? " = " + defaultValue : "");
        }

        public static string GetParameterAssignment(ParameterInfo pi)
        {
            // for observer, add type check code to ensure that a parameter is an instance of concrete interfaced observer.
            if (IsObserverInterface(pi.ParameterType))
                return $"{pi.Name} = ({GetObserverClassName(pi.ParameterType)}){pi.Name}";
            else
                return $"{pi.Name} = {pi.Name}";
        }

        public static bool HasNonTrivialDefaultValue(this ParameterInfo pi)
        {
            if (pi.HasDefaultValue == false || pi.DefaultValue == null)
                return false;

            if (pi.DefaultValue.GetType().IsValueType == false)
                return true;

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

            if (type.IsEnum)
                return string.Format("{0}.{1}", type.FullName, value);

            return string.Format("{0}", value);
        }

        public static IEnumerable<string> GetReachableMemebers(Type type, Func<Type, bool> filter)
        {
            // itself

            if (filter(type))
                yield return "";

            // members

            if (type.IsPrimitive)
                yield break;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (filter(field.FieldType))
                    yield return field.Name;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (filter(property.PropertyType) && property.GetGetMethod(false) != null)
                    yield return property.Name;
            }
        }
    }
}
