using System;
using System.Linq;
using System.Reflection;

namespace CodeGenerator
{
    public static class TypeExtensions
    {
        // Output: System.Collections.Generic.Dictionary<System.Int32, System.String>
        public static string GetSymbolDisplay(this Type type, bool isFullName = false, bool typeless = false)
        {
            if (type.IsGenericType)
            {
                var namespacePrefix = type.Namespace + (type.Namespace.Length > 0 ? "." : "");
                return (isFullName ? namespacePrefix : "") + type.GetPureName() + type.GetGenericParameters(typeless);
            }
            else
            {
                return isFullName ? (type.FullName ?? type.Name) : type.Name;
            }
        }

        // Output: where T : new(), IEquatable<T> where U : struct
        public static string GetGenericConstraintClause(this Type type)
        {
            if (type.IsGenericType == false && type.ContainsGenericParameters == false)
                return "";

            var constraints = type.GetTypeInfo().GenericTypeParameters.Select(t => t.GetParameterGenericConstraintClause()).Where(c => c.Any()).ToList();
            return constraints.Any()
                ? " " + string.Join(" ", constraints)
                : "";
        }

        // Output: where T : new(), IEquatable<T> where U : struct
        public static string GetGenericConstraintClause(this MethodInfo method)
        {
            if (method.IsGenericMethod == false && method.ContainsGenericParameters == false)
                return "";

            var constraints = method.GetGenericArguments().Select(t => t.GetParameterGenericConstraintClause()).Where(c => c.Any()).ToList();
            return constraints.Any()
                ? " " + string.Join(" ", constraints)
                : "";
        }

        // Output: where T : new(), IEquatable<T>
        public static string GetParameterGenericConstraintClause(this Type type)
        {
            var gpa = type.GenericParameterAttributes;
            var specialContrains = new[]
            {
                gpa.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) ? "class" : "",
                gpa.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) ? "struct" : "",
                gpa.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) ? "new()" : "",
            };

            var contraints = specialContrains.Where(t => t.Length > 0)
                                             .Concat(type.GetGenericParameterConstraints().Select(t => t.GetSymbolDisplay(true)))
                                             .ToList();
            return (contraints.Count > 0)
                ? string.Format("where {0} : {1}", type.Name, string.Join(", ", contraints))
                : "";
        }

        // Output: <T, U>
        public static string GetGenericParameters(this Type type, bool typeless = false)
        {
            if (type.IsGenericType == false)
                return "";
            var genericParams = type.GenericTypeArguments.Any()
                ? string.Join(", ", type.GenericTypeArguments.Select(t => typeless ? "" : t.GetSymbolDisplay(true)))
                : string.Join(", ", type.GetTypeInfo().GenericTypeParameters.Select(t => typeless ? "" : t.GetSymbolDisplay(true)));
            return "<" + genericParams + ">";
        }

        // Output: <T, U>
        public static string GetGenericParameters(this MethodInfo method, bool typeless = false)
        {
            return method.IsGenericMethod
              ? "<" + string.Join(", ", method.GetGenericArguments().Select(t => typeless ? "" : t.Name)) + ">"
              : "";
        }
    }
}
