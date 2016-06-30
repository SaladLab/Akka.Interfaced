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

            w._($"#region {type.GetSymbolDisplay(true)}");
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
                w._($"[ProtoMember(2)] public string Address;");
                w._();

                w._("[ProtoConverter]");
                using (w.B($"public static {surrogateClassName} Convert(IRequestTarget value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"var target = ((BoundActorTarget)value);");
                    w._($"return new {surrogateClassName} {{ Id = target.Id, Address = target.Address }};");
                }

                w._("[ProtoConverter]");
                using (w.B($"public static IRequestTarget Convert({surrogateClassName} value)"))
                {
                    w._($"if (value == null) return null;");
                    w._($"return new BoundActorTarget(value.Id, value.Address);");
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

            w._($"[PayloadTable(typeof({type.GetSymbolDisplay(typeless: true)}), PayloadTableKind.Request)]");
            using (w.B($"public static class {Utility.GetPayloadTableClassName(type)}{type.GetGenericParameters()}{type.GetGenericConstraintClause()}"))
            {
                // generate GetPayloadTypes method

                using (w.B("public static Type[,] GetPayloadTypes()"))
                {
                    using (w.i("return new Type[,] {", "};"))
                    {
                        foreach (var m in method2PayloadTypeNames)
                        {
                            var genericParameters = m.Item1.GetGenericParameters(typeless: true);
                            var payloadTypes = m.Item2;
                            var returnType = payloadTypes.Item2 != "" ? $"typeof({payloadTypes.Item2}{genericParameters})" : "null";
                            w._($"{{ typeof({payloadTypes.Item1}{genericParameters}), {returnType} }},");
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
                    using (w.B($"public class {payloadTypes.Item1}{method.GetGenericParameters()}",
                               $": IInterfacedPayload, IAsyncInvokable{tagOverridable}{observerUpdatable}{method.GetGenericConstraintClause()}"))
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
                                        ? $", DefaultValue({parameter.DefaultValue.GetValueLiteral()})"
                                        : "";
                                attr = $"[ProtoMember({i + 1}){defaultValueAttr}] ";

                                if (parameter.HasNonTrivialDefaultValue())
                                {
                                    defaultValueExpression = " = " + parameter.DefaultValue.GetValueLiteral();
                                }
                            }

                            var typeName = parameter.ParameterType.GetSymbolDisplay(true);
                            w._($"{attr}public {typeName} {parameter.Name}{defaultValueExpression};");
                        }
                        if (parameters.Any())
                            w._();

                        // GetInterfaceType

                        using (w.B($"public Type GetInterfaceType()"))
                        {
                            w._($"return typeof({type.GetSymbolDisplay()});");
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
                                    w._($"var __v = await (({type.GetSymbolDisplay()})__target).{method.Name}({parameterNames});",
                                        $"return (IValueGetable)(new {payloadTypes.Item2}{method.GetGenericParameters()} {{ v = __v }});");
                                }
                                else
                                {
                                    w._($"await (({type.GetSymbolDisplay()})__target).{method.Name}({parameterNames});",
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
                                    var typeName = tagParameter.ParameterType.GetSymbolDisplay(true);
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
                        using (w.B($"public class {payloadTypes.Item2}{method.GetGenericParameters()}",
                                   $": IInterfacedPayload, IValueGetable{actorRefUpdatable}{method.GetGenericConstraintClause()}"))
                        {
                            var attr = (Options.UseProtobuf) ? "[ProtoMember(1)] " : "";
                            w._($"{attr}public {returnType.GetSymbolDisplay(true)} v;");
                            w._();

                            // GetInterfaceType

                            using (w.B("public Type GetInterfaceType()"))
                            {
                                w._($"return typeof({type.GetSymbolDisplay()});");
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
            using (w.B($"public interface {Utility.GetNoReplyInterfaceName(type)}{type.GetGenericParameters()}{baseNoReplysInherit}"))
            {
                foreach (var m in typeInfos.First().Item2)
                {
                    var method = m.Item1;
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => p.GetParameterDeclaration(true)));
                    w._($"void {method.Name}{method.GetGenericParameters()}({paramStr}){method.GetGenericConstraintClause()};");
                }
            }

            // ActorRef

            var refClassName = Utility.GetActorRefClassName(type);
            var refClassGenericName = refClassName + type.GetGenericParameters();
            var noReplyInterfaceName = Utility.GetNoReplyInterfaceName(type);
            var noReplyInterfaceGenericName = noReplyInterfaceName + type.GetGenericParameters();

            using (w.B($"public class {refClassName}{type.GetGenericParameters()} : InterfacedActorRef, {type.GetSymbolDisplay()}, {noReplyInterfaceName}{type.GetGenericParameters()}{type.GetGenericConstraintClause()}"))
            {
                // InterfaceType property

                w._($"public override Type InterfaceType => typeof({type.GetSymbolDisplay()});");
                w._();

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

                // With Helpers

                using (w.B($"public {noReplyInterfaceGenericName} WithNoReply()"))
                {
                    w._("return this;");
                }

                using (w.B($"public {refClassGenericName} WithRequestWaiter(IRequestWaiter requestWaiter)"))
                {
                    w._($"return new {refClassGenericName}(Target, requestWaiter, Timeout);");
                }

                using (w.B($"public {refClassGenericName} WithTimeout(TimeSpan? timeout)"))
                {
                    w._($"return new {refClassGenericName}(Target, RequestWaiter, timeout);");
                }

                // IInterface message methods

                foreach (var t in typeInfos)
                {
                    var payloadTableClassName = Utility.GetPayloadTableClassName(t.Item1) + type.GetGenericParameters();

                    foreach (var m in t.Item2)
                    {
                        var method = m.Item1;
                        var payloadTypes = m.Item2;
                        var parameters = method.GetParameters();

                        var parameterTypeNames = string.Join(", ", parameters.Select(p => p.GetParameterDeclaration(true)));
                        var parameterInits = string.Join(", ", parameters.Select(Utility.GetParameterAssignment));
                        var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();

                        // Request Methods

                        var returnTaskType = (returnType != null) ? $"Task<{returnType.GetSymbolDisplay(true)}>" : "Task";
                        var prototype = $"public {returnTaskType} {method.Name}{method.GetGenericParameters()}({parameterTypeNames}){method.GetGenericConstraintClause()}";
                        using (w.B(prototype))
                        {
                            using (w.i("var requestMessage = new RequestMessage {", "};"))
                            {
                                w._($"InvokePayload = new {payloadTableClassName}.{payloadTypes.Item1}{method.GetGenericParameters()} {{ {parameterInits} }}");
                            }

                            if (returnType != null)
                                w._($"return SendRequestAndReceive<{returnType.GetSymbolDisplay(true)}>(requestMessage);");
                            else
                                w._($"return SendRequestAndWait(requestMessage);");
                        }
                    }
                }

                // IInterface_NoReply message methods

                foreach (var t in typeInfos)
                {
                    var interfaceName = Utility.GetNoReplyInterfaceName(t.Item1);
                    var interfaceGenericName = interfaceName + t.Item1.GetGenericParameters();

                    var payloadTableClassName = Utility.GetPayloadTableClassName(t.Item1) + type.GetGenericParameters();

                    foreach (var m in t.Item2)
                    {
                        var method = m.Item1;
                        var payloadTypes = m.Item2;
                        var parameters = method.GetParameters();

                        var parameterTypeNames = string.Join(", ", parameters.Select(p => p.GetParameterDeclaration(false)));
                        var parameterInits = string.Join(", ", parameters.Select(Utility.GetParameterAssignment));

                        // Request Methods

                        using (w.B($"void {interfaceGenericName}.{method.Name}{method.GetGenericParameters()}({parameterTypeNames})"))
                        {
                            using (w.i("var requestMessage = new RequestMessage {", "};"))
                            {
                                w._($"InvokePayload = new {payloadTableClassName}.{payloadTypes.Item1}{method.GetGenericParameters()} {{ {parameterInits} }}");
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
            w._($"[AlternativeInterface(typeof({type.GetSymbolDisplay(typeless: true)}))]");
            using (w.B($"public interface {Utility.GetActorSyncInterfaceName(type)}{type.GetGenericParameters()} : {baseSyncesInherit}{type.GetGenericConstraintClause()}"))
            {
                foreach (var m in typeInfos.First().Item2)
                {
                    var method = m.Item1;
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => p.GetParameterDeclaration(true)));
                    var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                    var returnTypeLiteral = (returnType != null) ? returnType.GetSymbolDisplay(true) : "void";
                    w._($"{returnTypeLiteral} {method.Name}{method.GetGenericParameters()}({paramStr}){method.GetGenericConstraintClause()};");
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
