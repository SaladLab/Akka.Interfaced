using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeGenerator
{
    public static class Utility
    {
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

        public static string GetParameterAssignment(ParameterInfo pi)
        {
            // for observer, add type check code to ensure that a parameter is an instance of concrete interfaced observer.
            if (IsObserverInterface(pi.ParameterType))
                return $"{pi.Name} = ({GetObserverClassName(pi.ParameterType)}{pi.ParameterType.GetGenericParameters()}){pi.Name}";
            else
                return $"{pi.Name} = {pi.Name}";
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
