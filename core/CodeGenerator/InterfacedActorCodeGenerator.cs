using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Akka.Interfaced;
using CodeWriter;

namespace CodeGen
{
    public class InterfacedActorCodeGenerator
    {
        private bool _surrogateForBoundActorRefGenerated;

        public Options Options { get; set; }

        public void GenerateCode(Type type, CodeWriter.CodeWriter w)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            if (Options.UseProtobuf && Options.UseSlimClient)
                EnsureSurrogateForBoundActorRef(w);

            w._($"#region {type.FullName}");
            w._();

            var namespaceHandle = (string.IsNullOrEmpty(type.Namespace) == false)
                ? w.B($"namespace {type.Namespace}")
                : null;

            // Collect all methods and make payload type name for each one

            var methods = GetInvokableMethods(type);
            var method2PayloadTypeNameMap = GetPayloadTypeNames(type, methods);

            // Generate all

            GeneratePayloadCode(type, w, methods, method2PayloadTypeNameMap);
            GenerateRefCode(type, w, methods, method2PayloadTypeNameMap);

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");
        }

        private void EnsureSurrogateForBoundActorRef(CodeWriter.CodeWriter w)
        {
            if (_surrogateForBoundActorRefGenerated)
                return;

            w._($"#region SurrogateForBoundActorRef");
            w._();

            var surrogateClassName = Utility.GetSurrogateClassName(typeof(BoundActorRef));

            w._("[ProtoContract]");
            using (w.B($"public class {surrogateClassName}"))
            {
                w._($"[ProtoMember(1)] public int Id;");
                w._();

                w._("[ProtoConverter]");
                using (w.B($"public static {surrogateClassName} Convert(BoundActorRef value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"return new {surrogateClassName} {{ Id = value.Id }};");
                }

                w._("[ProtoConverter]");
                using (w.B($"public static BoundActorRef Convert({surrogateClassName} value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"return new BoundActorRef(value.Id);");
                }
            }

            w._();
            w._($"#endregion");

            _surrogateForBoundActorRefGenerated = true;
        }

        private void GeneratePayloadCode(
            Type type, CodeWriter.CodeWriter w,
            MethodInfo[] methods, Dictionary<MethodInfo, Tuple<string, string>> method2PayloadTypeNameMap)
        {
            var tagName = Utility.GetActorInterfaceTagName(type);

            w._($"[PayloadTable(typeof({type.Name}), PayloadTableKind.Request)]");
            using (w.B($"public static class {Utility.GetPayloadTableClassName(type)}"))
            {
                // generate GetPayloadTypes method

                using (w.B("public static Type[,] GetPayloadTypes()"))
                {
                    using (w.i("return new Type[,] {", "};"))
                    {
                        foreach (var method in methods)
                        {
                            var typeName = method2PayloadTypeNameMap[method];
                            var returnType = typeName.Item2 != "" ? $"typeof({typeName.Item2})" : "null";
                            w._($"{{ typeof({typeName.Item1}), {returnType} }},");
                        }
                    }
                }

                // generate payload classes for all methods

                foreach (var method in methods)
                {
                    var payloadTypeName = method2PayloadTypeNameMap[method];
                    var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                    var observerParameters = method.GetParameters()
                        .Select(p => Tuple.Create(p, Utility.GetReachableMemebers(p.ParameterType, Utility.IsObserverInterface).ToArray()))
                        .Where(i => i.Item2.Length > 0)
                        .ToArray();

                    // Invoke payload

                    if (Options.UseProtobuf)
                        w._("[ProtoContract, TypeAlias]");

                    var tagOverridable = tagName != null ? ", IPayloadTagOverridable" : "";
                    var observerUpdatable = observerParameters.Any() ? ", IPayloadObserverUpdatable" : "";
                    using (w.B($"public class {payloadTypeName.Item1}",
                               $": IInterfacedPayload, IAsyncInvokable{tagOverridable}{observerUpdatable}"))
                    {
                        // Parameters

                        var parameters = method.GetParameters();
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            var parameter = parameters[i];

                            var attr = "";
                            var defaultValueExpression = "";
                            if (Options.UseProtobuf)
                            {
                                var defaultValueAttr =
                                    parameter.HasNonTrivialDefaultValue()
                                        ? $", DefaultValue({Utility.GetValueLiteral(parameter.DefaultValue)})"
                                        : "";
                                attr = $"[ProtoMember({i + 1}){defaultValueAttr}] ";

                                if (parameter.HasNonTrivialDefaultValue())
                                {
                                    defaultValueExpression = " = " + Utility.GetValueLiteral(parameter.DefaultValue);
                                }
                            }

                            var typeName = Utility.GetTypeName(parameter.ParameterType);
                            w._($"{attr}public {typeName} {parameter.Name}{defaultValueExpression};");
                        }
                        if (parameters.Any())
                            w._();

                        // GetInterfaceType

                        using (w.B($"public Type GetInterfaceType()"))
                        {
                            w._($"return typeof({type.Name});");
                        }

                        // InvokeAsync

                        if (Options.UseSlimClient)
                        {
                            using (w.B("public Task<IValueGetable> InvokeAsync(object __target)"))
                            {
                                w._("return null;");
                            }
                        }
                        else
                        {
                            using (w.B("public async Task<IValueGetable> InvokeAsync(object __target)"))
                            {
                                var parameterNames = string.Join(", ", method.GetParameters().Select(p => p.Name));
                                if (returnType != null)
                                {
                                    w._($"var __v = await (({type.Name})__target).{method.Name}({parameterNames});",
                                        $"return (IValueGetable)(new {payloadTypeName.Item2} {{ v = __v }});");
                                }
                                else
                                {
                                    w._($"await (({type.Name})__target).{method.Name}({parameterNames});",
                                        $"return null;");
                                }
                            }
                        }

                        // IPayloadTagOverridable.SetTag

                        if (tagName != null)
                        {
                            using (w.B($"void IPayloadTagOverridable.SetTag(object value)"))
                            {
                                var tagParameter = parameters.FirstOrDefault(pi => pi.Name == tagName);
                                if (tagParameter != null)
                                {
                                    var typeName = Utility.GetTypeName(tagParameter.ParameterType);
                                    w._($"{tagName} = ({typeName})value;");
                                }
                            }
                        }

                        // IPayloadObserverUpdatable.Update

                        if (observerParameters.Any())
                        {
                            using (w.B("void IPayloadObserverUpdatable.Update(Action<IInterfacedObserver> updater)"))
                            {
                                foreach (var p in observerParameters)
                                {
                                    using (w.b($"if ({p.Item1.Name} != null)"))
                                    {
                                        foreach (var m in p.Item2)
                                        {
                                            if (m == "")
                                                w._($"updater({p.Item1.Name});");
                                            else
                                                w._($"if ({p.Item1.Name}.{m} != null) updater({p.Item1.Name}.{m});");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Return payload

                    if (returnType != null)
                    {
                        var actorRefs = Utility.GetReachableMemebers(returnType, Utility.IsActorInterface).ToArray();

                        if (Options.UseProtobuf)
                            w._("[ProtoContract, TypeAlias]");

                        var actorRefUpdatable = actorRefs.Any() ? ", IPayloadActorRefUpdatable" : "";
                        using (w.B($"public class {payloadTypeName.Item2}",
                                   $": IInterfacedPayload, IValueGetable{actorRefUpdatable}"))
                        {
                            var attr = (Options.UseProtobuf) ? "[ProtoMember(1)] " : "";
                            w._($"{attr}public {Utility.GetTypeName(returnType)} v;");
                            w._();

                            // GetInterfaceType

                            using (w.B("public Type GetInterfaceType()"))
                            {
                                w._($"return typeof({type.Name});");
                            }

                            // IValueGetable.Value

                            using (w.B("public object Value"))
                            {
                                w._($"get {{ return v; }}");
                            }

                            // IPayloadActorRefUpdatable.Update

                            if (actorRefs.Any())
                            {
                                using (w.B("void IPayloadActorRefUpdatable.Update(Action<object> updater)"))
                                {
                                    using (w.b($"if (v != null)"))
                                    {
                                        foreach (var m in actorRefs)
                                        {
                                            if (m == "")
                                                w._($"updater(v); ");
                                            else
                                                w._($"if (v.{m} != null) updater(v.{m});");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateRefCode(
            Type type, CodeWriter.CodeWriter w,
            MethodInfo[] methods, Dictionary<MethodInfo, Tuple<string, string>> method2PayloadTypeNameMap)
        {
            // NoReply Interface

            using (w.B($"public interface {Utility.GetNoReplyInterfaceName(type)}"))
            {
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    w._($"void {method.Name}({paramStr});");
                }
            }

            // ActorRef

            var refClassName = Utility.GetActorRefClassName(type);
            var noReplyInterfaceName = Utility.GetNoReplyInterfaceName(type);
            var payloadTableClassName = Utility.GetPayloadTableClassName(type);

            using (w.B($"public class {refClassName} : InterfacedActorRef, {type.Name}, {noReplyInterfaceName}"))
            {
                // Constructors

                using (w.B($"public {refClassName}(IActorRef actor) : base(actor)"))
                {
                }

                using (w.B($"public {refClassName}(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout) : base(actor, requestWaiter, timeout)"))
                {
                }

                // With Helpers

                using (w.B($"public {noReplyInterfaceName} WithNoReply()"))
                {
                    w._("return this;");
                }

                using (w.B($"public {refClassName} WithRequestWaiter(IRequestWaiter requestWaiter)"))
                {
                    w._($"return new {refClassName}(Actor, requestWaiter, Timeout);");
                }

                using (w.B($"public {refClassName} WithTimeout(TimeSpan? timeout)"))
                {
                    w._($"return new {refClassName}(Actor, RequestWaiter, timeout);");
                }

                // IInterface message methods

                foreach (var method in methods)
                {
                    var messageName = method2PayloadTypeNameMap[method];
                    var parameters = method.GetParameters();

                    var parameterTypeNames = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + p.Name));
                    var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();

                    // Request Methods

                    var prototype = (returnType != null)
                        ? $"public Task<{Utility.GetTypeName(returnType)}> {method.Name}({parameterTypeNames})"
                        : $"public Task {method.Name}({parameterTypeNames})";

                    using (w.B(prototype))
                    {
                        using (w.i("var requestMessage = new RequestMessage {", "};"))
                        {
                            w._($"InvokePayload = new {payloadTableClassName}.{messageName.Item1} {{ {parameterInits} }}");
                        }

                        if (returnType != null)
                            w._($"return SendRequestAndReceive<{Utility.GetTypeName(returnType)}>(requestMessage);");
                        else
                            w._($"return SendRequestAndWait(requestMessage);");
                    }
                }

                // IInterface_NoReply message methods

                foreach (var method in methods)
                {
                    var messageName = method2PayloadTypeNameMap[method];
                    var parameters = method.GetParameters();

                    var parameterTypeNames = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, false)));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + p.Name));

                    // Request Methods

                    using (w.B($"void {noReplyInterfaceName}.{method.Name}({parameterTypeNames})"))
                    {
                        using (w.i("var requestMessage = new RequestMessage {", "};"))
                        {
                            w._($"InvokePayload = new {payloadTableClassName}.{messageName.Item1} {{ {parameterInits} }}");
                        }
                        w._("SendRequest(requestMessage);");
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
                    w._($"[ProtoMember(1)] public IActorRef Actor;");
                    w._();

                    w._("[ProtoConverter]");
                    using (w.B($"public static {surrogateClassName} Convert({type.Name} value)"))
                    {
                        w._($"if (value == null) return null;");
                        w._($"return new {surrogateClassName} {{ Actor = (({refClassName})value).Actor }};");
                    }

                    w._("[ProtoConverter]");
                    using (w.B($"public static {type.Name} Convert({surrogateClassName} value)"))
                    {
                        w._($"if (value == null) return null;");
                        w._($"return new {refClassName}(value.Actor);");
                    }
                }
            }
        }

        private MethodInfo[] GetInvokableMethods(Type type)
        {
            var methods = type.GetMethods();
            if (methods.Any(m => m.ReturnType.Name.StartsWith("Task") == false))
                throw new Exception(string.Format("All methods of {0} should return Task or Task<T>", type.FullName));
            return methods.OrderBy(m => m, new MethodInfoComparer()).ToArray();
        }

        private Dictionary<MethodInfo, Tuple<string, string>> GetPayloadTypeNames(Type type, MethodInfo[] methods)
        {
            var method2PayloadTypeNameMap = new Dictionary<MethodInfo, Tuple<string, string>>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                var ordinal = methods.Take(i).Count(m => m.Name == method.Name) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2PayloadTypeNameMap[method] = Tuple.Create(
                    string.Format("{0}{1}_Invoke", method.Name, ordinalStr),
                    returnType != null
                        ? string.Format("{0}{1}_Return", method.Name, ordinalStr)
                        : "");
            }
            return method2PayloadTypeNameMap;
        }
    }
}
