using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace CodeGen
{
    class InterfacedActorCodeGeneratorFull : InterfacedActorCodeGeneratorBase
    {
        public void GenerateCode(Type type, ICodeGenWriter writer)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            writer.PushRegion(type.FullName);
            writer.PushNamespace(type.Namespace);

            // Collect Method and make message name for each one

            var methods = GetMessageMethods(type);
            var method2MessageNameMap = GetMessageNameMap(type, methods);
            var tagName = Utility.GetActorInterfaceTagName(type);

            // Method message table
            {
                var sb = new StringBuilder();
                var className = type.Name + "__MessageTable";
                sb.AppendFormat("[MessageTableForInterfacedActor(typeof({0}))]\n", type.Name);
                sb.AppendFormat("public static class {0}\n", className);
                sb.Append("{\n");

                sb.AppendFormat("\tpublic static Type[,] GetMessageTypes()\n");
                sb.Append("\t{\n");
                sb.AppendFormat("\t\treturn new Type[,]\n");
                sb.Append("\t\t{\n");

                foreach (var method in methods)
                {
                    var messageName = method2MessageNameMap[method];
                    sb.AppendFormat("\t\t\t{{{0}, {1}}},\n", 
                        string.Format("typeof({0})", messageName.Item1),
                        messageName.Item2 != "" ? string.Format("typeof({0})", messageName.Item2) : "null");
                }

                sb.Append("\t\t};\n");
                sb.Append("\t}\n");
                sb.Append("}");

                writer.AddCode(sb.ToString());
            }

            // Method message classes

            foreach (var method in methods)
            {
                var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                var messageName = method2MessageNameMap[method];

                // Invoke message
                {
                    var sb = new StringBuilder();

                    if (Options.UseProtobuf)
                        sb.AppendFormat("[ProtoContract, TypeAlias]\n");

                    sb.AppendFormat("public class {0} : IInterfacedMessage, {1}IAsyncInvokable\n", 
                        messageName.Item1,
                        tagName != null ? "ITagOverridable, " : "");
                    sb.Append("{\n");
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
                                    ? string.Format(", DefaultValue({0})",
                                                    Utility.GetValueLiteral(parameter.DefaultValue))
                                    : "";
                            attr = string.Format("[ProtoMember({0}){1}] ", i + 1, defaultValueAttr);

                            if (parameter.HasNonTrivialDefaultValue())
                            {
                                defaultValueExpression = " = " + Utility.GetValueLiteral(parameter.DefaultValue);
                            }
                        }

                        sb.AppendFormat("\t{0}public {1} {2}{3};\n",
                                        attr, Utility.GetTransportTypeName(parameter.ParameterType),
                                        parameter.Name, defaultValueExpression);
                    }

                    if (parameters.Length > 0)
                        sb.AppendLine();
                    sb.AppendFormat("\tpublic Type GetInterfaceType() {{ return typeof({0}); }}\n", type.Name);

                    if (tagName != null)
                    {
                        sb.AppendLine();
                        var tagParameter = parameters.FirstOrDefault(pi => pi.Name == tagName);
                        if (tagParameter != null)
                        {
                            var setStatement = string.Format("{0} = ({1})value;", tagName, Utility.GetTransportTypeName(tagParameter.ParameterType));
                            sb.AppendFormat("\tpublic void SetTag(object value) {{ {0} }}\n", setStatement);
                        }
                        else
                        {
                            sb.AppendFormat("\tpublic void SetTag(object value) {{ }}\n");
                        }
                    }

                    var parameterNames = string.Join(", ", method.GetParameters().Select(p => p.Name));
                    sb.AppendLine();
                    sb.AppendFormat("\tpublic async Task<IValueGetable> Invoke(object target)\n");
                    sb.Append("\t{\n");
                    if (returnType != null)
                    {
                        sb.AppendFormat("\t\tvar __v = await(({0})target).{1}({2});\n", 
                            type.Name, method.Name, parameterNames);
                        sb.AppendFormat("\t\treturn (IValueGetable)(new {0} {{ v = {1}__v }});\n",
                            messageName.Item2, Utility.GetTransportTypeCasting(returnType));
                    }
                    else
                    {
                        sb.AppendFormat("\t\tawait (({0})target).{1}({2});\n", type.Name, method.Name, parameterNames);
                        sb.AppendFormat("\t\treturn null;\n");
                    }
                    sb.Append("\t}\n");
                    sb.Append("}");
                    writer.AddCode(sb.ToString());
                }

                // Return message
                if (returnType != null)
                {
                    var sb = new StringBuilder();

                    if (Options.UseProtobuf)
                        sb.AppendFormat("[ProtoContract, TypeAlias]\n");

                    sb.AppendFormat("public class {0} : IInterfacedMessage, IValueGetable\n", messageName.Item2);
                    sb.Append("{\n");

                    var attr = (Options.UseProtobuf) ? "[ProtoMember(1)] " : "";
                    sb.AppendFormat("\t{0}public {1} v;\n", attr, Utility.GetTransportTypeName(returnType));

                    sb.AppendLine();
                    sb.AppendFormat("\tpublic Type GetInterfaceType() {{ return typeof({0}); }}\n", type.Name);

                    sb.Append("\n");
                    sb.AppendFormat("\tpublic object Value {{ get {{ return v; }} }}\n");

                    sb.Append("}");
                    writer.AddCode(sb.ToString());
                }
            }

            // NoReply Interface
            {
                var sb = new StringBuilder();
                var interfaceName = Utility.GetNoReplyInterfaceName(type);

                sb.AppendFormat("public interface {0}\n", interfaceName);
                sb.Append("{\n");

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    sb.AppendFormat("\tvoid {0}({1});\n", method.Name, paramStr);
                }

                sb.Append("}");
                writer.AddCode(sb.ToString());
            }

            // ActorRef
            {
                var sb = new StringBuilder();
                var refClassName = Utility.GetActorRefClassName(type);
                var noReplyInterfaceName = Utility.GetNoReplyInterfaceName(type);

                if (Options.UseProtobuf)
                    sb.AppendFormat("[ProtoContract, TypeAlias]\n");

                sb.AppendFormat("public class {0} : InterfacedActorRef, {1}, {2}\n",
                                refClassName, type.Name, noReplyInterfaceName);
                sb.Append("{\n");

                // Protobuf-net specialized

                if (Options.UseProtobuf)
                {
                    sb.Append("\t[ProtoMember(1)] private ActorRefBase _actor\n");
                    sb.Append("\t{\n");
                    sb.Append("\t\tget { return (ActorRefBase)Actor; }\n");
                    sb.Append("\t\tset { Actor = value; }\n");
                    sb.Append("\t}\n");
                    sb.Append("\n");
                    sb.AppendFormat("\tprivate {0}()\n", refClassName);
                    sb.AppendFormat("\t\t: base(null)\n");
                    sb.Append("\t{\n");
                    sb.Append("\t}\n");
                    sb.Append("\n");
                }

                // Constructor

                sb.AppendFormat("\tpublic {0}(IActorRef actor)\n", refClassName);
                sb.AppendFormat("\t\t: base(actor)\n");
                sb.Append("\t{\n");
                sb.Append("\t}\n");

                // Constructor (detailed one)

                sb.Append("\n");
                sb.AppendFormat("\tpublic {0}(IActorRef actor, IRequestWaiter requestWaiter, TimeSpan? timeout)\n", refClassName);
                sb.AppendFormat("\t\t: base(actor, requestWaiter, timeout)\n");
                sb.Append("\t{\n");
                sb.Append("\t}\n");

                // WithNoReply

                sb.Append("\n");
                sb.AppendFormat("\tpublic {0} WithNoReply()\n", noReplyInterfaceName);
                sb.Append("\t{\n");
                sb.AppendFormat("\t\treturn this;\n");
                sb.Append("\t}\n");

                // WithRequestWaiter

                sb.Append("\n");
                sb.AppendFormat("\tpublic {0} WithRequestWaiter(IRequestWaiter requestWaiter)\n", refClassName);
                sb.Append("\t{\n");
                sb.AppendFormat("\t\treturn new {0}(Actor, requestWaiter, Timeout);\n", refClassName);
                sb.Append("\t}\n");

                // WithTimeout

                sb.Append("\n");
                sb.AppendFormat("\tpublic {0} WithTimeout(TimeSpan? timeout)\n", refClassName);
                sb.Append("\t{\n");
                sb.AppendFormat("\t\treturn new {0}(Actor, RequestWaiter, timeout);\n", refClassName);
                sb.Append("\t}\n");

                // IInterface message methods

                foreach (var method in methods)
                {
                    var messageName = method2MessageNameMap[method];
                    var parameters = method.GetParameters();

                    var parameterNames = string.Join(", ", parameters.Select(p => p.Name));
                    var parameterTypeNames = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, true)));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + Utility.GetTransportTypeCasting(p.ParameterType) + p.Name));
                    var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();

                    // Request Methods

                    sb.Append("\n");

                    if (returnType != null)
                        sb.AppendFormat("\tpublic Task<{0}> {1}({2})\n", Utility.GetTypeName(returnType), method.Name, parameterTypeNames);
                    else
                        sb.AppendFormat("\tpublic Task {0}({1})\n", method.Name, parameterTypeNames);

                    sb.Append("\t{\n");

                    sb.AppendFormat("\t\tvar requestMessage = new RequestMessage\n");
                    sb.Append("\t\t{\n");
                    sb.AppendFormat("\t\t\tMessage = new {0} {{ {1} }}\n", messageName.Item1, parameterInits);
                    sb.Append("\t\t};\n");

                    if (returnType != null)
                        sb.AppendFormat("\t\treturn SendRequestAndReceive<{0}>(requestMessage);\n", Utility.GetTypeName(returnType));
                    else
                        sb.AppendFormat("\t\treturn SendRequestAndWait(requestMessage);\n");

                    sb.Append("\t}\n");
                }

                // IInterface_NoReply message methods

                foreach (var method in methods)
                {
                    var messageName = method2MessageNameMap[method];
                    var parameters = method.GetParameters();

                    var parameterTypeNames = string.Join(", ", parameters.Select(p => Utility.GetParameterDeclaration(p, false)));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + Utility.GetTransportTypeCasting(p.ParameterType) + p.Name));

                    // Request Methods

                    sb.Append("\n");
                    sb.AppendFormat("\tvoid {0}.{1}({2})\n",
                                    noReplyInterfaceName, method.Name, parameterTypeNames);

                    sb.Append("\t{\n");

                    sb.AppendFormat("\t\tvar requestMessage = new RequestMessage\n");
                    sb.Append("\t\t{\n");
                    sb.AppendFormat("\t\t\tMessage = new {0} {{ {1} }}\n", messageName.Item1, parameterInits);
                    sb.Append("\t\t};\n");

                    sb.AppendFormat("\t\tSendRequest(requestMessage);\n");

                    sb.Append("\t}\n");
                }

                sb.Append("}");
                writer.AddCode(sb.ToString());
            }

            writer.PopNamespace();
            writer.PopRegion();
        }
    }
}
