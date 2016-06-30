using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeGenerator
{
    public static class TypeExtensions
    {
        // Example: System.String -> string
        public static string GetSpecialTypeName(this Type type)
        {
            if (type == typeof(void))
                return "void";
            if (type == typeof(sbyte))
                return "sbyte";
            if (type == typeof(short))
                return "short";
            if (type == typeof(int))
                return "int";
            if (type == typeof(long))
                return "long";
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(ushort))
                return "ushort";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(decimal))
                return "decimal";
            if (type == typeof(char))
                return "char";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(string))
                return "string";
            if (type == typeof(object))
                return "object";
            return null;
        }

        // Example: List<T> -> List
        public static string GetPureName(this Type type)
        {
            if (type.IsGenericType)
            {
                var delimiterPos = type.Name.IndexOf('`');
                return type.Name.Substring(0, delimiterPos);
            }
            else
            {
                return GetSpecialTypeName(type) ?? type.Name;
            }
        }

        // Example: List<T> -> List_1
        public static string GetSafeName(this Type type)
        {
            if (type.IsGenericType)
            {
                return type.Name.Replace('`', '_');
            }
            else
            {
                return GetSpecialTypeName(type) ?? type.Name;
            }
        }

        // Example: Dictionary<int, string> -> System.Collections.Generic.Dictionary<System.Int32, System.String>
        public static string GetSymbolDisplay(this Type type, bool isFullName = false, bool typeless = false)
        {
            if (type.IsGenericType)
            {
                var namespacePrefix = type.Namespace + (type.Namespace.Length > 0 ? "." : "");
                return (isFullName ? namespacePrefix : "") + type.GetPureName() + type.GetGenericParameters(typeless);
            }
            else
            {
                return GetSpecialTypeName(type) ?? (isFullName ? (type.FullName ?? type.Name) : type.Name);
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

        // Output: "Literal"
        public static string GetValueLiteral(this object value)
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

        public static string GetParameterDeclaration(this ParameterInfo pi, bool includeDefaultExpression)
        {
            var defaultValue = pi.HasDefaultValue ? GetValueLiteral(pi.DefaultValue) : "";
            return (pi.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") +
                   (pi.ParameterType.GetSymbolDisplay(true) + " " + pi.Name) +
                   (includeDefaultExpression && defaultValue != "" ? " = " + defaultValue : "");
        }

        public static bool HasNonTrivialDefaultValue(this ParameterInfo pi)
        {
            if (pi.HasDefaultValue == false || pi.DefaultValue == null)
                return false;

            if (pi.DefaultValue.GetType().IsValueType == false)
                return true;

            return pi.DefaultValue.Equals(Activator.CreateInstance(pi.ParameterType)) == false;
        }
    }
}
