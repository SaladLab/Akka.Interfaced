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
    public class InterfacedObserverCodeGenerator
    {
        public Options Options { get; set; }

        public void GenerateCode(Type type, ICodeGenWriter writer)
        {
            Console.WriteLine("GenerateCode: " + type.FullName);

            writer.PushRegion(type.FullName);

            if (string.IsNullOrEmpty(type.Namespace) == false)
                writer.PushNamespace(type.Namespace);

            // Collect Method and make message name for each one

            var methods = GetEventMethods(type);
            var method2PayloadTypeNameMap = GetPayloadTypeNames(type, methods);

            // Generate all

            GeneratePayloadCode(type, writer, methods, method2PayloadTypeNameMap);
            GenerateObserverCode(type, writer, methods, method2PayloadTypeNameMap);

            if (string.IsNullOrEmpty(type.Namespace) == false)
                writer.PopNamespace();

            writer.PopRegion();
        }

        private void GeneratePayloadCode(
            Type type, ICodeGenWriter writer,
            MethodInfo[] methods, Dictionary<MethodInfo, string> method2PayloadTypeNameMap)
        {
            var sb = new StringBuilder();
            var className = Utility.GetPayloadTableClassName(type);

            sb.AppendFormat("public static class {0}\n", className);
            sb.Append("{\n");

            foreach (var method in methods)
            {
                var payloadTypeName = method2PayloadTypeNameMap[method];

                // Invoke payload
                {
                    if (method != methods[0])
                        sb.Append("\n");

                    if (Options.UseProtobuf)
                        sb.AppendFormat("\t[ProtoContract, TypeAlias]\n");

                    sb.AppendFormat("\tpublic class {0} : IInvokable\n", payloadTypeName);
                    sb.Append("\t{\n");
                    var parameters = method.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var attr = (Options.UseProtobuf) ? string.Format("[ProtoMember({0})] ", i + 1) : "";
                        sb.AppendFormat("\t\t{0}public {1} {2};\n", attr, Utility.GetTypeName(parameter.ParameterType), parameter.Name);
                    }

                    var parameterNames = string.Join(", ", method.GetParameters().Select(p => p.Name));
                    if (string.IsNullOrEmpty(parameterNames) == false)
                        sb.AppendLine();

                    sb.AppendFormat("\t\tpublic void Invoke(object target)\n");
                    sb.Append("\t\t{\n");
                    sb.AppendFormat("\t\t\t(({0})target).{1}({2});\n", type.Name, method.Name, parameterNames);
                    sb.Append("\t\t}\n");
                    sb.Append("\t}\n");
                }
            }

            sb.Append("}");

            writer.AddCode(sb.ToString());
        }

        private void GenerateObserverCode(
            Type type, ICodeGenWriter writer,
            MethodInfo[] methods, Dictionary<MethodInfo, string> method2PayloadTypeNameMap)
        {
            if (Options.UseSlimClient)
                return;

            var sb = new StringBuilder();
            var className = Utility.GetObserverClassName(type);
            var payloadTableClassName = Utility.GetPayloadTableClassName(type);

            if (Options.UseProtobuf)
                sb.AppendFormat("[ProtoContract, TypeAlias]\n");

            sb.AppendFormat("public class {0} : InterfacedObserver, {1}\n", className, type.Name);
            sb.Append("{\n");

            // Protobuf-net specialized

            if (Options.UseProtobuf)
            {
                sb.Append("\t[ProtoMember(1)] private ActorRefBase _actor\n");
                sb.Append("\t{\n");
                sb.Append("\t\tget { return Channel != null ? (ActorRefBase)(((ActorNotificationChannel)Channel).Actor) : null; }\n");
                sb.Append("\t\tset { Channel = new ActorNotificationChannel(value); }\n");
                sb.Append("\t}\n");
                sb.Append("\n");
                sb.Append("\t[ProtoMember(2)] private int _observerId\n");
                sb.Append("\t{\n");
                sb.Append("\t\tget { return ObserverId; }\n");
                sb.Append("\t\tset { ObserverId = value; }\n");
                sb.Append("\t}\n");
                sb.Append("\n");
                sb.AppendFormat("\tprivate {0}()\n", className);
                sb.AppendFormat("\t\t: base(null, 0)\n");
                sb.Append("\t{\n");
                sb.Append("\t}\n");
                sb.Append("\n");
            }

            // Constructor (IActorRef)

            sb.AppendFormat("\tpublic {0}(IActorRef target, int observerId)\n", className);
            sb.AppendFormat("\t\t: base(new ActorNotificationChannel(target), observerId)\n");
            sb.Append("\t{\n");
            sb.Append("\t}\n");

            // Constructor (INotificationChannel)

            sb.Append("\n");
            sb.AppendFormat("\tpublic {0}(INotificationChannel channel, int observerId)\n", className);
            sb.AppendFormat("\t\t: base(channel, observerId)\n");
            sb.Append("\t{\n");
            sb.Append("\t}\n");

            // Observer method messages

            foreach (var method in methods)
            {
                var messageName = method2PayloadTypeNameMap[method];
                var parameters = method.GetParameters();

                var parameterNames = string.Join(", ", parameters.Select(p => p.Name));
                var parameterTypeNames = string.Join(", ", parameters.Select(p => (p.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") + Utility.GetTypeName(p.ParameterType) + " " + p.Name));
                var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + p.Name));

                // Request Methods

                sb.Append("\n");

                sb.AppendFormat("\tpublic void {0}({1})\n", method.Name, parameterTypeNames);

                sb.Append("\t{\n");

                sb.AppendFormat("\t\tvar payload = new {0}.{1} {{ {2} }};\n",
                                payloadTableClassName, messageName, parameterInits);
                sb.AppendFormat("\t\tNotify(payload);\n");
                sb.Append("\t}\n");
            }

            sb.Append("}");
            writer.AddCode(sb.ToString());
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
