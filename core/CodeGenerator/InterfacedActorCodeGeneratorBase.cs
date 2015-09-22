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
    abstract class InterfacedActorCodeGeneratorBase
    {
        public Options Options { get; set; }

        #region MessageTable

        protected MethodInfo[] GetMessageMethods(Type type)
        {
            var methods = type.GetMethods();
            if (methods.Any(m => m.ReturnType.Name.StartsWith("Task") == false))
                throw new Exception(string.Format("All methods of {0} should return Task or Task<T>", type.FullName));
            return methods;
        }

        protected Dictionary<MethodInfo, Tuple<string, string>> GetMessageNameMap(Type type, MethodInfo[] methods)
        {
            var method2MessageNameMap = new Dictionary<MethodInfo, Tuple<string, string>>();
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                var returnType = method.ReturnType.GenericTypeArguments.FirstOrDefault();
                var ordinal = methods.Take(i).Count(m => m.Name == method.Name) + 1;
                var ordinalStr = (ordinal <= 1) ? "" : string.Format("_{0}", ordinal);

                method2MessageNameMap[method] = Tuple.Create(
                    string.Format("{0}__{1}{2}__Invoke", type.Name, method.Name, ordinalStr),
                    returnType != null
                        ? string.Format("{0}__{1}{2}__Return", type.Name, method.Name, ordinalStr)
                        : "");
            }
            return method2MessageNameMap;
        }

        #endregion
    }
}
