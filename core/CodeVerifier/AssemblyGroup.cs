using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace CodeVerifier
{
    internal class AssemblyGroup
    {
        private string _basePath;
        private string _mainPath;
        private Dictionary<string, AssemblyDefinition> _assemblyMap;

        public Dictionary<string, AssemblyDefinition> AssemblyMap
        {
            get { return _assemblyMap; }
        }

        public IEnumerable<TypeDefinition> Types
        {
            get { return _assemblyMap.Values.SelectMany(a => a.MainModule.Types); }
        }

        public AssemblyGroup(string mainPath)
        {
            _basePath = Path.GetDirectoryName(mainPath);
            _mainPath = mainPath;
            _assemblyMap = new Dictionary<string, AssemblyDefinition>();

            ReadAssemblyRecursive(_mainPath);
        }

        private void ReadAssemblyRecursive(string path)
        {
            try
            {
                var lib = AssemblyDefinition.ReadAssembly(path);
                _assemblyMap[lib.FullName] = lib;
                foreach (var dep in lib.MainModule.AssemblyReferences)
                {
                    if (_assemblyMap.ContainsKey(dep.FullName) == false)
                    {
                        ReadAssemblyRecursive(Path.Combine(_basePath, dep.Name + ".dll"));
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        public TypeDefinition GetType(string fullName)
        {
            foreach (var type in Types)
            {
                if (type.FullName == fullName)
                    return type;
            }
            return null;
        }

        public TypeDefinition GetType(TypeReference type)
        {
            var asmName = type.Scope.ToString();

            var moduleScope = type.Scope as ModuleDefinition;
            if (moduleScope != null)
                asmName = moduleScope.Assembly.FullName;

            AssemblyDefinition asmDef;
            if (_assemblyMap.TryGetValue(asmName, out asmDef) == false)
                return null;

            foreach (var typeDef in asmDef.MainModule.GetTypes())
            {
                if (typeDef.FullName == type.FullName)
                    return typeDef;
            }
            return null;
        }
    }
}
