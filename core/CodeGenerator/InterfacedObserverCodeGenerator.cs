using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeWriter;
using Akka.Interfaced;

namespace CodeGen
{
    public class InterfacedObserverCodeGenerator
    {
        private bool _surrogateForINotificationChannelGenerated;

        public Options Options { get; set; }

        public void GenerateCode(Type type, CodeWriter.CodeWriter w)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            if (Options.UseProtobuf && Options.UseSlimClient)
                EnsureSurrogateForINotificationChannel(type, w);

            w._($"#region {type.FullName}");
            w._();

            var namespaceHandle = (string.IsNullOrEmpty(type.Namespace) == false)
                ? w.B($"namespace {type.Namespace}")
                : null;

            // Collect all methods and make payload type name for each one

            var baseTypes = type.GetInterfaces().Where(t => t.FullName != "Akka.Interfaced.IInterfacedObserver").ToArray();
            var infos = new List<Tuple<Type, List<Tuple<MethodInfo, string>>>>();
            foreach (var t in new[] { type }.Concat(baseTypes))
            {
                var methods = GetEventMethods(t);
                infos.Add(Tuple.Create(t, GetPayloadTypeNames(t, methods)));
            }

            // Generate all

            GeneratePayloadCode(type, w, infos.First().Item2);
            GenerateObserverCode(type, w, baseTypes, infos.ToArray());
            if (Options.UseSlimClient == false)
                GenerateAsyncCode(type, w, baseTypes, infos.ToArray());

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");
        }

        private void EnsureSurrogateForINotificationChannel(Type callerType, CodeWriter.CodeWriter w)
        {
            if (_surrogateForINotificationChannelGenerated)
                return;

            var namespaceHandle = (string.IsNullOrEmpty(callerType.Namespace) == false)
                ? w.B($"namespace {callerType.Namespace}")
                : null;

            var surrogateClassName = Utility.GetSurrogateClassName("INotificationChannel");

            w._($"#region {surrogateClassName}");
            w._();

            w._("[ProtoContract]");
            using (w.B($"public class {surrogateClassName}"))
            {
                w._("[ProtoConverter]");
                using (w.B($"public static {surrogateClassName} Convert(INotificationChannel value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"return new {surrogateClassName}();");
                }

                w._("[ProtoConverter]");
                using (w.B($"public static INotificationChannel Convert({surrogateClassName} value)"))
                {
                    w._($"return null;");
                }
            }

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");

            _surrogateForINotificationChannelGenerated = true;
        }

        private void GeneratePayloadCode(
            Type type, CodeWriter.CodeWriter w,
            List<Tuple<MethodInfo, string>> method2PayloadTypeNames)
        {
            w._($"[PayloadTable(typeof({type.Name}), PayloadTableKind.Notification)]");
            using (w.B($"public static class {Utility.GetPayloadTableClassName(type)}"))
            {
                // generate GetPayloadTypes method

                using (w.B("public static Type[] GetPayloadTypes()"))
                {
                    using (w.i("return new Type[] {", "};"))
                    {
                        foreach (var m in method2PayloadTypeNames)
                        {
                            w._($"typeof({m.Item2}),");
                        }
                    }
                }

                // generate payload classes for all methods

                foreach (var m in method2PayloadTypeNames)
                {
                    var method = m.Item1;
                    var payloadTypeName = m.Item2;

                    // Invoke payload
                    {
                        if (Options.UseProtobuf)
                            w._("[ProtoContract, TypeAlias]");

                        using (w.B($"public class {payloadTypeName} : IInterfacedPayload, IInvokable"))
                        {
                            // Parameters

                            var parameters = method.GetParameters();
                            for (var i = 0; i < parameters.Length; i++)
                            {
                                var parameter = parameters[i];
                                var attr = (Options.UseProtobuf) ? string.Format("[ProtoMember({0})] ", i + 1) : "";
                                w._($"{attr}public {Utility.GetTypeName(parameter.ParameterType)} {parameter.Name};");
                            }
                            if (parameters.Any())
                                w._();

                            // GetInterfaceType

                            using (w.B("public Type GetInterfaceType()"))
                            {
                                w._($"return typeof({type.Name});");
                            }

                            // Invoke

                            using (w.B("public void Invoke(object __target)"))
                            {
                                var parameterNames = string.Join(", ", method.GetParameters().Select(p => p.Name));
                                w._($"(({type.Name})__target).{method.Name}({parameterNames});");
                            }
                        }
                    }
                }
            }
        }

        private void GenerateObserverCode(
            Type type, CodeWriter.CodeWriter w, Type[] baseTypes,
            Tuple<Type, List<Tuple<MethodInfo, string>>>[] typeInfos)
        {
            var className = Utility.GetObserverClassName(type);

            using (w.B($"public class {className} : InterfacedObserver, {type.Name}"))
            {
                // Constructor

                using (w.B($"public {className}()",
                           $": base(null, 0)"))
                {
                }

                // Constructor (INotificationChannel)

                using (w.B($"public {className}(INotificationChannel channel, int observerId = 0)",
                           $": base(channel, observerId)"))
                {
                }

                // Observer method messages

                foreach (var t in typeInfos)
                {
                    var payloadTableClassName = Utility.GetPayloadTableClassName(t.Item1);

                    foreach (var m in t.Item2)
                    {
                        var method = m.Item1;
                        var payloadType = m.Item2;
                        var parameters = method.GetParameters();

                        var parameterNames = string.Join(", ", parameters.Select(p => p.Name));
                        var parameterTypeNames = string.Join(", ", parameters.Select(p => (p.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") + Utility.GetTypeName(p.ParameterType) + " " + p.Name));
                        var parameterInits = string.Join(", ", parameters.Select(Utility.GetParameterAssignment));

                        // Request Methods

                        using (w.B($"public void {method.Name}({parameterTypeNames})"))
                        {
                            w._($"var payload = new {payloadTableClassName}.{payloadType} {{ {parameterInits} }};",
                                $"Notify(payload);");
                        }
                    }
                }
            }

            // Protobuf-net specialized

            if (Options.UseProtobuf)
            {
                var surrogateClassName = Utility.GetSurrogateClassName(type);

                w._("[ProtoContract]");
                using (w.B($"public class {surrogateClassName}"))
                {
                    w._("[ProtoMember(1)] public INotificationChannel Channel;");
                    w._("[ProtoMember(2)] public int ObserverId;");
                    w._();

                    w._("[ProtoConverter]");
                    using (w.B($"public static {surrogateClassName} Convert({type.Name} value)"))
                    {
                        w._($"if (value == null) return null;");
                        w._($"var o = ({className})value;");
                        w._($"return new {surrogateClassName} {{ Channel = o.Channel, ObserverId = o.ObserverId }};");
                    }

                    w._("[ProtoConverter]");
                    using (w.B($"public static {type.Name} Convert({surrogateClassName} value)"))
                    {
                        w._($"if (value == null) return null;");
                        w._($"return new {className}(value.Channel, value.ObserverId);");
                    }
                }
            }
        }

        private void GenerateAsyncCode(
            Type type, CodeWriter.CodeWriter w, Type[] baseTypes,
            Tuple<Type, List<Tuple<MethodInfo, string>>>[] typeInfos)
        {
            // NoReply Interface

            var baseSynces = baseTypes.Select(t => Utility.GetObserverAsyncInterfaceName(t));
            var baseSyncesInherit = baseSynces.Any() ? string.Join(", ", baseSynces) : "IInterfacedObserver";
            w._($"[AlternativeInterface(typeof({type.Name}))]");
            using (w.B($"public interface {Utility.GetObserverAsyncInterfaceName(type)} : {baseSyncesInherit}"))
            {
                foreach (var m in typeInfos.First().Item2)
                {
                    var method = m.Item1;
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    w._($"Task {method.Name}({paramStr});");
                }
            }
        }

        private MethodInfo[] GetEventMethods(Type type)
        {
            var methods = type.GetMethods();
            var wrongMethods = methods.Where(m => m.ReturnType.Name.StartsWith("Void") == false).ToArray();
            if (wrongMethods.Any())
                throw new Exception(string.Format("All methods of {0} should return void instead of {1}", type.FullName, wrongMethods[0].ReturnType.Name));
            return methods.OrderBy(m => m, new MethodInfoComparer()).ToArray();
        }

        private List<Tuple<MethodInfo, string>> GetPayloadTypeNames(Type type, MethodInfo[] methods)
        {
            var method2PayloadTypeNames = new List<Tuple<MethodInfo, string>>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var ordinal = methods.Take(i).Count(m => m.Name == method.Name) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2PayloadTypeNames.Add(Tuple.Create(
                    method,
                    string.Format("{0}{1}_Invoke", method.Name, ordinalStr)));
            }
            return method2PayloadTypeNames;
        }
    }
}
