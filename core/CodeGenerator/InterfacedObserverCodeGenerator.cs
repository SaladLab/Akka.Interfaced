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
    class InterfacedObserverCodeGeneratorFull
    {
        public Options Options { get; set; }

        #region MessageTable

        protected MethodInfo[] GetMessageMethods(Type type)
        {
            var methods = type.GetMethods();
            var wrongMethods = methods.Where(m => m.ReturnType.Name.StartsWith("Void") == false).ToArray();
            if (wrongMethods.Any())
                throw new Exception(string.Format("All methods of {0} should return void instead of {1}", type.FullName, wrongMethods[0].ReturnType.Name));
            return methods;
        }

        protected Dictionary<MethodInfo, string> GetMessageNameMap(Type type, MethodInfo[] methods)
        {
            var method2MessageNameMap = new Dictionary<MethodInfo, string>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var ordinal = methods.Take(i).Count(m => m.Name == method.Name) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2MessageNameMap[method] = string.Format(
                    "{0}__{1}{2}__Invoke", type.Name, method.Name, ordinalStr);
            }
            return method2MessageNameMap;
        }

        #endregion

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

                    sb.AppendFormat("public class {0} : IInvokable\n", messageName);
                    sb.Append("{\n");
                    var parameters = method.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var attr = (Options.UseProtobuf) ? string.Format("[ProtoMember({0})] ", i + 1) : "";
                        sb.AppendFormat("\t{0}public {1} {2};\n", attr, Utility.GetTypeName(parameter.ParameterType), parameter.Name);
                    }

                    var parameterNames = string.Join(", ", method.GetParameters().Select(p => p.Name));
                    if (string.IsNullOrEmpty(parameterNames) == false)
                        sb.AppendLine();

                    sb.AppendFormat("\tpublic void Invoke(object target)\n");
                    sb.Append("\t{\n");
                    sb.AppendFormat("\t\t(({0})target).{1}({2});\n", type.Name, method.Name, parameterNames);
                    sb.Append("\t}\n");
                    sb.Append("}");
                    writer.AddCode(sb.ToString());
                }
            }

            // Observer
            if (Options.UseSlimClient == false)
            {
                var sb = new StringBuilder();
                var className = Utility.GetObserverClassName(type);

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

                // Constructor

                sb.AppendFormat("\tpublic {0}(INotificationChannel channel, int observerId)\n", className);
                sb.AppendFormat("\t\t: base(channel, observerId)\n");
                sb.Append("\t{\n");
                sb.Append("\t}\n");

                foreach (var method in methods)
                {
                    var messageName = method2MessageNameMap[method];
                    var parameters = method.GetParameters();

                    var parameterNames = string.Join(", ", parameters.Select(p => p.Name));
                    var parameterTypeNames = string.Join(", ", parameters.Select(p => (p.GetCustomAttribute<ParamArrayAttribute>() != null ? "params " : "") + Utility.GetTypeName(p.ParameterType) + " " + p.Name));
                    var parameterInits = string.Join(", ", parameters.Select(p => p.Name + " = " + p.Name));

                    // Request Methods

                    sb.Append("\n");

                    sb.AppendFormat("\tpublic void {0}({1})\n", method.Name, parameterTypeNames);

                    sb.Append("\t{\n");

                    sb.AppendFormat("\t\tvar message = new {0} {{ {1} }};\n", messageName, parameterInits);
                    sb.AppendFormat("\t\tNotify(message);\n");
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
