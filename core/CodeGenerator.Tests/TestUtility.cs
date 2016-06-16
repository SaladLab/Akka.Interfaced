using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Interfaced;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace CodeGenerator
{
    public static class TestUtility
    {
        public static CSharpCompilation Generate(Options options, Type[] types, ITestOutputHelper output = null)
        {
            // generate code from types

            var generator = new EntryCodeGenerator(options);
            generator.GenerateCode(types);
            var code = generator.CodeWriter.ToString();
            if (output != null)
            {
                var typeInfo = string.Join(", ", types.Select(t => t.Name));
                output.WriteLine($"***** Generated Code({typeInfo}) *****");
                output.WriteLine(code);
                output.WriteLine("");
            }

            // compile generated code

            output?.WriteLine($"***** Compile Code *****");

            var parseOption = new CSharpParseOptions(LanguageVersion.CSharp6, DocumentationMode.Parse, SourceCodeKind.Regular);
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(code, parseOption, "Generated.cs") };
            var references = new[] { typeof(object), typeof(IInterfacedActor), typeof(InterfacedActor), typeof(IActorRef), typeof(IGreeter) }
                .Select(t => MetadataReference.CreateFromFile(t.Assembly.Location));

            var compilation = CSharpCompilation.Create(
               "Generated.dll",
               syntaxTrees: syntaxTrees,
               references: references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        var line = diagnostic.Location.GetLineSpan();
                        output?.WriteLine("{0}({1}): {2} {3}",
                            line.Path,
                            line.StartLinePosition.Line + 1,
                            diagnostic.Id,
                            diagnostic.GetMessage());
                    }
                }

                Assert.True(result.Success, "Build error!");
            }

            return compilation;
        }

        public static IEnumerable<ISymbol> GetTypeSymbols(this CSharpCompilation compilation)
        {
            return compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type);
        }

        public static IEnumerable<string> GetTypeSymbolNames(this CSharpCompilation compilation)
        {
            return compilation.GetTypeSymbols().Select(s => s.Name);
        }
    }
}
