using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace CodeVerifier
{
    internal class ExtendedInterfaceVerifier
    {
        private Options _options;

        public List<string> VerifiedTypes { get; private set; }
        public List<string> Errors { get; private set; }

        public ExtendedInterfaceVerifier(Options options)
        {
            _options = options;

            VerifiedTypes = new List<string>();
            Errors = new List<string>();
        }

        public void Verify(AssemblyGroup asmGroup, string typeName = null)
        {
            foreach (var type in asmGroup.Types)
            {
                if (typeName != null && typeName != type.FullName)
                    continue;

                var extendedInterfaces = type.Interfaces.Where(ifs => ifs.FullName.StartsWith("Akka.Interfaced.IExtendedInterface")).ToList();
                if (extendedInterfaces.Any())
                {
                    VerifiedTypes.Add(type.FullName);

                    var interfaceTypes = new List<TypeDefinition>();
                    foreach (var ifs in extendedInterfaces)
                    {
                        foreach (var arg in ((GenericInstanceType)ifs).GenericArguments)
                        {
                            var typeDef = asmGroup.GetType(arg);
                            if (typeDef != null)
                                interfaceTypes.Add(typeDef);
                            else
                                Errors.Add($"Cannot get type {arg.FullName} for {type.FullName}");
                        }
                    }
                    VerifyInterface(type, interfaceTypes.ToArray());
                }
            }
        }

        internal void VerifyInterface(TypeDefinition implementType, TypeDefinition[] interfaceTypes)
        {
            // get extended method from implement type

            var targetMethods =
                implementType.Methods
                    .Select(m => Tuple.Create(m, m.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Akka.Interfaced.ExtendedHandlerAttribute")))
                    .Where(i => i.Item2 != null)
                    .ToList();

            // find method and remove it

            foreach (var ifs in interfaceTypes)
            {
                foreach (var interfaceMethod in ifs.Methods)
                {
                    var name = interfaceMethod.Name;
                    var parameters = interfaceMethod.Parameters;

                    MethodDefinition targetMethod = null;
                    foreach (var method in targetMethods)
                    {
                        var handlerArg = GetHandlerArgument(method.Item2);
                        if (handlerArg != null)
                        {
                            // check tagged method
                            if (handlerArg.Item1 != null && handlerArg.Item1.FullName != ifs.FullName)
                                continue;
                            if (handlerArg.Item2 != null && handlerArg.Item2 != name)
                                continue;
                        }
                        else if (method.Item1.Name != name)
                        {
                            // check method
                            continue;
                        }

                        if (AreParameterTypesEqual(method.Item1.Parameters, parameters))
                        {
                            if (targetMethod != null)
                            {
                                Errors.Add(
                                    $"Ambiguous handlers for {ifs.FullName}.{interfaceMethod.Name} method.\n" +
                                    $" {targetMethod.Name}\n {method.Item1.Name}\n");
                            }
                            targetMethod = method.Item1;
                        }
                    }
                    if (targetMethod == null)
                    {
                        Errors.Add(
                            $"Cannot find handler for {ifs.FullName}.{interfaceMethod.Name}");
                    }
                    targetMethods.RemoveAll(x => x.Item1 == targetMethod);
                }
            }

            // check all methods are used

            if (targetMethods.Any())
            {
                foreach (var method in targetMethods)
                {
                    Errors.Add($"Unused extended handler: {implementType.FullName}.{method.Item1.Name}");
                }
            }
        }

        internal static Tuple<TypeDefinition, string> GetHandlerArgument(CustomAttribute attr)
        {
            if (attr.ConstructorArguments.Count < 2)
                return null;

            // return arguments of ctor of ExtendedHandlerAttribute
            return Tuple.Create((TypeDefinition)attr.ConstructorArguments[0].Value, (string)attr.ConstructorArguments[1].Value);
        }

        private static bool AreParameterTypesEqual(Collection<ParameterDefinition> a, Collection<ParameterDefinition> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].ParameterType.FullName != b[i].ParameterType.FullName)
                    return false;
            }
            return true;
        }
    }
}
