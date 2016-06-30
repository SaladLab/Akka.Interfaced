using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeGenerator
{
    public static class Utility
    {
        public static string GetReferenceDisplay(this Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetPureName() + "<" + new string(',', type.GetTypeInfo().GenericTypeParameters.Count() - 1) + ">";
            }
            else
            {
                return type.Name;
            }
        }

        public static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericParams = string.Join(", ", type.GenericTypeArguments.Select(t => GetTypeName(t)));
                var delimiterPos = type.Name.IndexOf('`');
                return type.Namespace + "." + type.Name.Substring(0, delimiterPos) + "<" + genericParams + ">";
            }
            else if (type.IsGenericParameter)
            {
                return type.Name;
            }
            else
            {
                return type.FullName;
            }
        }

        public static string GetIdFromType(Type type)
        {
            if (type.IsGenericType)
            {
                return type.Name.Replace('`', '_');
            }
            else
            {
                return type.Name;
            }
        }

        public static string GetPureName(this Type type)
        {
            if (type.IsGenericType)
            {
                var delimiterPos = type.Name.IndexOf('`');
                return type.Name.Substring(0, delimiterPos);
            }
            else
            {
                return type.Name;
            }
        }

        public static string GetGenericParameterDeclaration(this Type type)
        {
            return type.IsGenericType
                ? "<" + string.Join(", ", type.GetTypeInfo().GenericTypeParameters.Select(t => t.Name)) + ">"
                : "";
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
            return type.GetPureName() + "_PayloadTable";
        }

        public static string GetNoReplyInterfaceName(Type type)
        {
            return type.GetPureName() + "_NoReply";
        }

        public static string GetActorRefClassName(Type type)
        {
            // because user will type this type, no _ prefix.
            return type.GetPureName().Substring(1) + "Ref";
        }

        public static string GetActorSyncInterfaceName(Type type)
        {
            // because user will type this type, no _ prefix.
            return type.GetPureName() + "Sync";
        }

        public static string GetObserverClassName(Type type)
        {
            return type.GetPureName().Substring(1);
        }

        public static string GetObserverAsyncInterfaceName(Type type)
        {
            // because user will type this type, no _ prefix.
            return type.GetPureName() + "Async";
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
            var literal = SymbolDisplay.FormatPrimitive(value, true, false);

            if (value != null)
            {
                var type = value.GetType();
                if (type.IsEnum)
                    return $"({type.FullName}){literal}";
                if (type == typeof(float))
                    return literal + "f";
            }

            return literal;
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
