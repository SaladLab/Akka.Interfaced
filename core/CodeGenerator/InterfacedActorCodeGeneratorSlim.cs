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
    class InterfacedActorCodeGeneratorSlim : InterfacedActorCodeGeneratorBase
    {
        public void GenerateCode(Type type, ICodeGenWriter writer)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            writer.PushRegion(type.FullName);
            writer.PushNamespace(type.Namespace);

            // Collect Method and make message name for each one

            var methods = GetMessageMethods(type);
            var method2MessageNameMap = GetMessageNameMap(type, methods);

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

                    sb.AppendFormat("public class {0} : IInterfacedMessage\n", messageName.Item1);
                    sb.Append("{\n");
                    var parameters = method.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var attr = (Options.UseProtobuf) ? string.Format("[ProtoMember({0})] ", i + 1) : "";
                        sb.AppendFormat("\t{0}public {1} {2};\n",
                            attr, Utility.GetTransportTypeName(parameter.ParameterType), parameter.Name);
                    }

                    if (parameters.Length > 0)
                        sb.AppendLine();
                    sb.AppendFormat("\tpublic Type GetInterfaceType() {{ return typeof({0}); }}\n", type.Name);

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

                sb.AppendFormat("public class {0} : InterfacedSlimActorRef, {1}\n",
                                refClassName, noReplyInterfaceName);
                sb.Append("{\n");

                // Constructor (detailed one)

                sb.AppendFormat("\tpublic {0}(ISlimActorRef actor, ISlimRequestWaiter requestWaiter, TimeSpan? timeout)\n", refClassName);
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
                sb.AppendFormat("\tpublic {0} WithRequestWaiter(ISlimRequestWaiter requestWaiter)\n", refClassName);
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

                    sb.AppendFormat("\t\tvar requestMessage = new SlimRequestMessage\n");
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

                    sb.AppendFormat("\t\tvar requestMessage = new SlimRequestMessage\n");
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
