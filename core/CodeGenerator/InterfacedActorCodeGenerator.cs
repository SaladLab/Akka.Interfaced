using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeWriter;

namespace CodeGenerator
{
    public class InterfacedActorCodeGenerator
    {
        private bool _surrogateForIActorRefGenerated;

        public Options Options { get; set; }

        public void GenerateCode(Type type, CodeWriter.CodeWriter w)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            if (Options.UseProtobuf && Options.UseSlimClient)
                EnsureSurrogateForIRequestTarget(type, w);

            w._($"#region {type.FullName}");
            w._();

            var namespaceHandle = (string.IsNullOrEmpty(type.Namespace) == false)
                ? w.B($"namespace {type.Namespace}")
                : null;

            // Collect all methods and make payload type name for each one

            var baseTypes = type.GetInterfaces().Where(t => t.FullName != "Akka.Interfaced.IInterfacedActor").ToArray();
            var infos = new List<Tuple<Type, List<Tuple<MethodInfo, Tuple<string, string>>>>>();
            foreach (var t in new[] { type }.Concat(baseTypes))
            {
                var methods = GetInvokableMethods(t);
                var method2PayloadTypeNameMap = GetPayloadTypeNames(t, methods);
                infos.Add(Tuple.Create(t, GetPayloadTypeNames(t, methods)));
            }

            // Generate all

            GeneratePayloadCode(type, w, infos.First().Item2);
            GenerateRefCode(type, w, baseTypes, infos.ToArray());
            if (Options.UseSlimClient == false)
                GenerateSyncCode(type, w, baseTypes, infos.ToArray());

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");
        }

        private void EnsureSurrogateForIRequestTarget(Type callerType, CodeWriter.CodeWriter w)
        {
            if (_surrogateForIActorRefGenerated)
                return;

            var surrogateClassName = Utility.GetSurrogateClassName("IRequestTarget");

            w._($"#region {surrogateClassName}");
            w._();

            var namespaceHandle = (string.IsNullOrEmpty(callerType.Namespace) == false)
                ? w.B($"namespace {callerType.Namespace}")
                : null;

            w._("[ProtoContract]");
            using (w.B($"public class {surrogateClassName}"))
            {
                w._($"[ProtoMember(1)] public int Id;");
                w._();

                w._("[ProtoConverter]");
                using (w.B($"public static {surrogateClassName} Convert(IRequestTarget value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"return new {surrogateClassName} {{ Id = ((BoundActorTarget)value).Id }};");
                }

                w._("[ProtoConverter]");
                using (w.B($"public static IRequestTarget Convert({surrogateClassName} value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"return new BoundActorTarget(value.Id);");
                }
            }

            namespaceHandle?.Dispose();

            w._();
            w._($"#endregion");

            _surrogateForIActorRefGenerated = true;
        }

        private void GeneratePayloadCode(
            Type type, CodeWriter.CodeWriter w,
            List<Tuple<MethodInfo, Tuple<string, string>>> method2PayloadTypeNames)
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
                        foreach (var m in method2PayloadTypeNames)
                        {
                            var payloadTypes = m.Item2;
                            var returnType = payloadTypes.Item2 != "" ? $"typeof({payloadTypes.Item2})" : "null";
                            w._($"{{ typeof({payloadTypes.Item1}), {returnType} }},");
                        }
                    }
                }

                // generate payload classes for all methods

                foreach (var m in method2PayloadTypeNames)
                {
                    var method = m.Item1;
                    var payloadTypes = m.Item2;
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
                    using (w.B($"public class {payloadTypes.Item1}",
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
                                        $"return (IValueGetable)(new {payloadTypes.Item2} {{ v = __v }});");
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
                                        foreach (var o in p.Item2)
                                        {
                                            if (o == "")
                                                w._($"updater({p.Item1.Name});");
                                            else
                                                w._($"if ({p.Item1.Name}.{o} != null) updater({p.Item1.Name}.{o});");
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
                        using (w.B($"public class {payloadTypes.Item2}",
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
                                        foreach (var r in actorRefs)
                                        {
                                            if (r == "")
                                                w._($"updater(v); ");
                                            else
                                                w._($"if (v.{r} != null) updater(v.{r});");
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
            Type type, CodeWriter.CodeWriter w, Type[] baseTypes,
            Tuple<Type, List<Tuple<MethodInfo, Tuple<string, string>>>>[] typeInfos)
        {
            // NoReply Interface

            var baseNoReplys = baseTypes.Select(t => Utility.GetNoReplyInterfaceName(t));
            var baseNoReplysInherit = baseNoReplys.Any() ? " : " + string.Join(", ", baseNoReplys) : "";
            using (w.B($"public interface {Utility.GetNoReplyInterfaceName(type)}{baseNoReplysInherit}"))
            {
                foreach (var m in typeInfos.First().Item2)
                {
                    var method = m.Item1;
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    w._($"void {method.Name}({paramStr});");
                }
            }

            // ActorRef

            var refClassName = Utility.GetActorRefClassName(type);
            var noReplyInterfaceName = Utility.GetNoReplyInterfaceName(type);

            using (w.B($"public class {refClassName} : InterfacedActorRef, {type.Name}, {noReplyInterfaceName}"))
            {
                // Constructors

                using (w.B($"public {refClassName}() : base(null)"))
                {
                }

                using (w.B($"public {refClassName}(IRequestTarget target) : base(target)"))
                {
                }

                using (w.B($"public {refClassName}(IRequestTarget target, IRequestWaiter requestWaiter, TimeSpan? timeout = null) : base(target, requestWaiter, timeout)"))
                {
                }

                // For IActorRef

                if (Options.UseSlimClient == false)
                {
                    using (w.B($"public {refClassName}(IActorRef actor) : base(new AkkaActorTarget(actor))"))
                    {
                    }

                    using (w.B($"public {refClassName}(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout = null) : base(new AkkaActorTarget(actor), requestWaiter, timeout)"))
                    {
                    }

                    w._($"public IActorRef Actor => ((AkkaActorTarget)Target)?.Actor;");
                    w._();
                }

                // With Helpers

                using (w.B($"public {noReplyInterfaceName} WithNoReply()"))
                {
                    w._("return this;");
                }

                using (w.B($"public {refClassName} WithRequestWaiter(IRequestWaiter requestWaiter)"))
                {
                    w._($"return new {refClassName}(Target, requestWaiter, Timeout);");
                }

                using (w.B($"public {refClassName} WithTimeout(TimeSpan? timeout)"))
                {
                    w._($"return new {refClassName}(Target, RequestWaiter, timeout);");
                }

                // IInterface message methods

                foreach (var t in typeInfos)
                {
                    var payloadTableClassName = Utility.GetPayloadTableClassName(t.Item1);

                    foreach (var m in t.Item2)
                    {
                        var method = m.Item1;
                        var payloadTypes = m.Item2;
                        var parameters = method.GetParameters();

                        var parameterTypeNames = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                        var parameterInits = string.Join(", ", parameters.Select(Utility.GetParameterAssignment));
                        var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();

                        // Request Methods

                        var prototype = (returnType != null)
                            ? $"public Task<{Utility.GetTypeName(returnType)}> {method.Name}({parameterTypeNames})"
                            : $"public Task {method.Name}({parameterTypeNames})";

                        using (w.B(prototype))
                        {
                            using (w.i("var requestMessage = new RequestMessage {", "};"))
                            {
                                w._($"InvokePayload = new {payloadTableClassName}.{payloadTypes.Item1} {{ {parameterInits} }}");
                            }

                            if (returnType != null)
                                w._($"return SendRequestAndReceive<{Utility.GetTypeName(returnType)}>(requestMessage);");
                            else
                                w._($"return SendRequestAndWait(requestMessage);");
                        }
                    }
                }

                // IInterface_NoReply message methods

                foreach (var t in typeInfos)
                {
                    var interfaceName = Utility.GetNoReplyInterfaceName(t.Item1);
                    var payloadTableClassName = Utility.GetPayloadTableClassName(t.Item1);

                    foreach (var m in t.Item2)
                    {
                        var method = m.Item1;
                        var payloadTypes = m.Item2;
                        var parameters = method.GetParameters();

                        var parameterTypeNames = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, false)));
                        var parameterInits = string.Join(", ", parameters.Select(Utility.GetParameterAssignment));

                        // Request Methods

                        using (w.B($"void {interfaceName}.{method.Name}({parameterTypeNames})"))
                        {
                            using (w.i("var requestMessage = new RequestMessage {", "};"))
                            {
                                w._($"InvokePayload = new {payloadTableClassName}.{payloadTypes.Item1} {{ {parameterInits} }}");
                            }
                            w._("SendRequest(requestMessage);");
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
                    w._($"[ProtoMember(1)] public IRequestTarget Target;");
                    w._();

                    w._("[ProtoConverter]");
                    using (w.B($"public static {surrogateClassName} Convert({type.Name} value)"))
                    {
                        w._($"if (value == null) return null;");
                        w._($"return new {surrogateClassName} {{ Target = (({refClassName})value).Target }};");
                    }

                    w._("[ProtoConverter]");
                    using (w.B($"public static {type.Name} Convert({surrogateClassName} value)"))
                    {
                        w._($"if (value == null) return null;");
                        w._($"return new {refClassName}(value.Target);");
                    }
                }
            }
        }

        private void GenerateSyncCode(
            Type type, CodeWriter.CodeWriter w, Type[] baseTypes,
            Tuple<Type, List<Tuple<MethodInfo, Tuple<string, string>>>>[] typeInfos)
        {
            // NoReply Interface

            var baseSynces = baseTypes.Select(t => Utility.GetActorSyncInterfaceName(t));
            var baseSyncesInherit = baseSynces.Any() ? string.Join(", ", baseSynces) : "IInterfacedActorSync";
            w._($"[AlternativeInterface(typeof({type.Name}))]");
            using (w.B($"public interface {Utility.GetActorSyncInterfaceName(type)} : {baseSyncesInherit}"))
            {
                foreach (var m in typeInfos.First().Item2)
                {
                    var method = m.Item1;
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                    var returnTypeLiteral = (returnType != null) ? Utility.GetTypeName(returnType) : "void";
                    w._($"{returnTypeLiteral} {method.Name}({paramStr});");
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

        private List<Tuple<MethodInfo, Tuple<string, string>>> GetPayloadTypeNames(Type type, MethodInfo[] methods)
        {
            var method2PayloadTypeNames = new List<Tuple<MethodInfo, Tuple<string, string>>>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                var ordinal = methods.Take(i).Count(m => m.Name == method.Name) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2PayloadTypeNames.Add(Tuple.Create(method, Tuple.Create(
                    string.Format("{0}{1}_Invoke", method.Name, ordinalStr),
                    returnType != null
                        ? string.Format("{0}{1}_Return", method.Name, ordinalStr)
                        : "")));
            }
            return method2PayloadTypeNames;
        }
    }
}
