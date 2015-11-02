using System;
using System.Reflection;
using Akka.Actor;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.Linq;

namespace Akka.Interfaced.ProtobufSerializer
{
    public static class AutoSurrogate
    {
        public static void Register(RuntimeTypeModel typeModel)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetTypesSafely(assembly))
                {
                    if (type.IsClass == false && type.IsValueType == false)
                        continue;

                    if (Attribute.GetCustomAttribute(type, typeof(ProtoBuf.ProtoContractAttribute)) == null)
                        continue;

                    var loweredTypeName = type.Name.ToLower();
                    if (loweredTypeName.Contains("surrogatedirectives"))
                    {
                        foreach (var field in type.GetFields())
                        {
                            var sourceType = FindSurrogateSourceType(field.FieldType);
                            if (sourceType != null)
                            {
                                try
                                {
                                    typeModel.Add(sourceType, false).SetSurrogate(field.FieldType);
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }
                        }
                    }
                    else if (type.Name.ToLower().Contains("surrogate"))
                    {
                        var sourceType = FindSurrogateSourceType(type);
                        if (sourceType != null)
                        {
                            try
                            {
                                typeModel.Add(sourceType, false).SetSurrogate(type);
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }

        private static Type FindSurrogateSourceType(Type type)
        {
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var m in type.GetMethods(flags))
            {
                var parameters = m.GetParameters();
                if (parameters.Length == 1 && m.ReturnType.Name != "Void" && m.ReturnType != type)
                {
                    // if (Attribute.GetCustomAttribute(m, typeof(ProtoBuf.ProtoConverterAttribute)) != null)
                    //     return m.ReturnType;
                    if (m.Name == "op_Implicit" || m.Name == "op_Explicit")
                        return m.ReturnType;
                }
            }
            return null;
        }
    }
}
