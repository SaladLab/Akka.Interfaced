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
        public Options Options { get; set; }

        public void GenerateCode(Type type, CodeWriter.CodeWriter w)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

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

        private void GeneratePayloadCode(
            Type type, CodeWriter.CodeWriter w,
            MethodInfo[] methods, Dictionary<MethodInfo, Tuple<string, string>> method2PayloadTypeNameMap)
        {
            var tagName = Utility.GetActorInterfaceTagName(type);

            var sb = new StringBuilder();
            var className = Utility.GetPayloadTableClassName(type);

            w._($"[PayloadTableForInterfacedActor(typeof({type.Name}))]");
            using (w.B($"public static class {className}"))
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

                    // Invoke payload

                    if (Options.UseProtobuf)
                        w._("[ProtoContract, TypeAlias]");

                    var tagOverridable = tagName != null ? "ITagOverridable, " : "";
                    using (w.B($"public class {payloadTypeName.Item1}",
                               $": IInterfacedPayload, {tagOverridable}IAsyncInvokable"))
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

                            var typeName = Utility.GetTransportTypeName(parameter.ParameterType);
                            w._($"{attr}public {typeName} {parameter.Name}{defaultValueExpression};");
                        }

                        // GetInterfaceType

                        if (parameters.Length > 0)
                            sb.AppendLine();
                        w._($"public Type GetInterfaceType() {{ return typeof({type.Name}); }}");

                        // SetTag

                        if (tagName != null)
                        {
                            sb.AppendLine();
                            var tagParameter = parameters.FirstOrDefault(pi => pi.Name == tagName);
                            if (tagParameter != null)
                            {
                                var typeName = Utility.GetTransportTypeName(tagParameter.ParameterType);
                                var setStatement = $"{tagName} = ({typeName})value;";
                                w._($"public void SetTag(object value) {{ {setStatement} }}");
                            }
                            else
                            {
                                w._($"public void SetTag(object value) {{ }}");
                            }
                        }

                        // InvokeAsync

                        sb.AppendLine();
                        if (Options.UseSlimClient)
                        {
                            using (w.B("public Task<IValueGetable> InvokeAsync(object target)"))
                            {
                                w._("return null;");
                            }
                        }
                        else
                        {
                            using (w.B("public async Task<IValueGetable> InvokeAsync(object target)"))
                            {
                                var parameterNames = string.Join(", ", method.GetParameters().Select(p => p.Name));
                                if (returnType != null)
                                {
                                    w._($"var __v = await (({type.Name})target).{method.Name}({parameterNames});");
                                    w._($"return (IValueGetable)(new {payloadTypeName.Item2} {{ v = {Utility.GetTransportTypeCasting(returnType)}__v }});");
                                }
                                else
                                {
                                    w._($"await (({type.Name})target).{method.Name}({parameterNames});");
                                    w._($"return null;");
                                }
                            }
                        }
                    }

                    // Return payload

                    if (returnType != null)
                    {
                        if (Options.UseProtobuf)
                            w._("[ProtoContract, TypeAlias]");

                        using (w.B($"public class {payloadTypeName.Item2}",
                                   $": IInterfacedPayload, IValueGetable"))
                        {
                            var attr = (Options.UseProtobuf) ? "[ProtoMember(1)] " : "";
                            w._($"{attr}public {Utility.GetTransportTypeName(returnType)} v;",
                                $"public Type GetInterfaceType() {{ return typeof({type.Name}); }}",
                                $"public object Value {{ get {{ return v; }} }}");
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

            if (Options.UseProtobuf && Options.UseSlimClient == false)
                w._("[ProtoContract, TypeAlias]");

            using (w.B($"public class {refClassName} : InterfacedActorRef, {type.Name}, {noReplyInterfaceName}"))
            {
                // Protobuf-net specialized

                if (Options.UseProtobuf && Options.UseSlimClient == false)
                {
                    using (w.B("[ProtoMember(1)] private ActorRefBase _actor"))
                    {
                        w._("get { return (ActorRefBase)Actor; }");
                        w._("set { Actor = value; }");
                    }

                    using (w.B($"private {refClassName}() : base(null)"))
                    {
                    }
                }

                // Constructor

                if (Options.UseSlimClient == false)
                {
                    using (w.B($"public {refClassName}(IActorRef actor) : base(actor)"))
                    {
                    }
                }

                // Constructor (detailed one)

                using (w.B($"public {refClassName}(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout) : base(actor, requestWaiter, timeout)"))
                {
                }

                // WithNoReply

                using (w.B($"public {noReplyInterfaceName} WithNoReply()"))
                {
                    w._("return this;");
                }

                // WithRequestWaiter

                using (w.B($"public {refClassName} WithRequestWaiter(IRequestWaiter requestWaiter)"))
                {
                    w._($"return new {refClassName}(Actor, requestWaiter, Timeout);");
                }

                // WithTimeout

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
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + Utility.GetTransportTypeCasting(p.ParameterType) + p.Name));
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
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + Utility.GetTransportTypeCasting(p.ParameterType) + p.Name));

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
