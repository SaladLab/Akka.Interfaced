using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeWriter;

namespace CodeGen
{
    public class InterfacedObserverCodeGenerator
    {
        public Options Options { get; set; }

        public void GenerateCode(Type type, CodeWriter.CodeWriter w)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            w._($"#region {type.FullName}");
            w._();

            var namespaceHandle = (string.IsNullOrEmpty(type.Namespace) == false)
                ? w.B($"namespace {type.Namespace}")
                : null;

            // Collect Method and make message name for each one

            var methods = GetEventMethods(type);
            var method2PayloadTypeNameMap = GetPayloadTypeNames(type, methods);

            // Generate all

            GeneratePayloadCode(type, w, methods, method2PayloadTypeNameMap);
            GenerateObserverCode(type, w, methods, method2PayloadTypeNameMap);

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");
        }

        private void GeneratePayloadCode(
            Type type, CodeWriter.CodeWriter w,
            MethodInfo[] methods, Dictionary<MethodInfo, string> method2PayloadTypeNameMap)
        {
            w._($"[PayloadTable(typeof({type.Name}), PayloadTableKind.Notification)]");
            using (w.B($"public static class {Utility.GetPayloadTableClassName(type)}"))
            {
                // generate GetPayloadTypes method

                using (w.B("public static Type[] GetPayloadTypes()"))
                {
                    using (w.i("return new Type[] {", "};"))
                    {
                        foreach (var method in methods)
                        {
                            var typeName = method2PayloadTypeNameMap[method];
                            w._($"typeof({typeName}),");
                        }
                    }
                }

                // generate payload classes for all methods

                foreach (var method in methods)
                {
                    var payloadTypeName = method2PayloadTypeNameMap[method];

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

                            // GetInterfaceType

                            w._($"public Type GetInterfaceType() {{ return typeof({type.Name}); }}");

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
            Type type, CodeWriter.CodeWriter w,
            MethodInfo[] methods, Dictionary<MethodInfo, string> method2PayloadTypeNameMap)
        {
            if (Options.UseSlimClient)
                return;

            var className = Utility.GetObserverClassName(type);
            var payloadTableClassName = Utility.GetPayloadTableClassName(type);

            if (Options.UseProtobuf)
                w._("[ProtoContract, TypeAlias]");

            using (w.B($"public class {className} : InterfacedObserver, {type.Name}"))
            {
                // Protobuf-net specialized

                if (Options.UseProtobuf)
                {
                    using (w.B("[ProtoMember(1)] private ActorRefBase _actor"))
                    {
                        w._("get { return Channel != null ? (ActorRefBase)(((ActorNotificationChannel)Channel).Actor) : null; }",
                            "set { Channel = new ActorNotificationChannel(value); }");
                    }

                    using (w.B("[ProtoMember(2)] private int _observerId"))
                    {
                        w._("get { return ObserverId; }",
                            "set { ObserverId = value; }");
                    }

                    using (w.B($"private {className}() : base(null, 0)"))
                    {
                    }
                }

                // Constructor (IActorRef)

                using (w.B($"public {className}(IActorRef target, int observerId = 0)",
                           $": base(new ActorNotificationChannel(target), observerId)"))
                {
                }

                // Constructor (INotificationChannel)

                using (w.B($"public {className}(INotificationChannel channel, int observerId = 0)",
                           $": base(channel, observerId)"))
                {
                }

                // Observer method messages

                foreach (var method in methods)
                {
                    var messageName = method2PayloadTypeNameMap[method];
                    var parameters = method.GetParameters();

                    var parameterNames = string.Join(", ", parameters.Select(p => p.Name));
                    var parameterTypeNames = string.Join(", ", parameters.Select(p => (p.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") + Utility.GetTypeName(p.ParameterType) + " " + p.Name));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + p.Name));

                    // Request Methods

                    using (w.B($"public void {method.Name}({parameterTypeNames})"))
                    {
                        w._($"var payload = new {payloadTableClassName}.{messageName} {{ {parameterInits} }};",
                            $"Notify(payload);");
                    }
                }
            }
        }

        private MethodInfo[] GetEventMethods(Type type)
        {
            var methods = type.GetMethods();
            var wrongMethods = methods.Where(m => m.ReturnType.Name.StartsWith("Void") == false).ToArray();
            if (wrongMethods.Any())
                throw new Exception(string.Format("All methods of {0} should return void instead of {1}", type.FullName, wrongMethods[0].ReturnType.Name));
            return methods;
        }

        private Dictionary<MethodInfo, string> GetPayloadTypeNames(Type type, MethodInfo[] methods)
        {
            var method2PayloadTypeNameMap = new Dictionary<MethodInfo, string>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var ordinal = methods.Take(i).Count(m => m.Name == method.Name) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2PayloadTypeNameMap[method] = string.Format(
                    "{0}{1}_Invoke", method.Name, ordinalStr);
            }
            return method2PayloadTypeNameMap;
        }
    }
}
