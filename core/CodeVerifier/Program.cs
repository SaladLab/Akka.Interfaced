using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Mono.Cecil;

namespace CodeVerifier
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            // Parse command line options

            if (args.Length == 1 && args[0].StartsWith("@"))
            {
                var argFile = args[0].Substring(1);
                if (File.Exists(argFile))
                {
                    args = File.ReadAllLines(argFile);
                }
                else
                {
                    Console.WriteLine("File not found: " + argFile);
                    return 1;
                }
            }

            var parser = new Parser(config => config.HelpWriter = Console.Out);
            if (args.Length == 0)
            {
                parser.ParseArguments<Options>(new[] { "--help" });
                return 1;
            }

            Options options = null;
            var result = parser.ParseArguments<Options>(args)
                .WithParsed(r => { options = r; });

            // Run process !

            if (options != null)
                return Process(options);
            else
                return 1;
        }

        private static int Process(Options options)
        {
            try
            {
                Console.WriteLine("Start Verify: " + options.Input);

                var asmGroup = new AssemblyGroup(options.Input);
                var verifier = new ExtendedInterfaceVerifier(options);
                verifier.Verify(asmGroup);

                if (options.Verbose)
                {
                    foreach (var type in verifier.VerifiedTypes)
                    {
                        Console.WriteLine("VerifiedType: " + type);
                    }
                }

                if (verifier.Errors.Any())
                {
                    foreach (var error in verifier.Errors)
                    {
                        Console.WriteLine("! " + error);
                    }
                    return 1;
                }

                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in processing:\n" + e);
                return 1;
            }
        }
    }
}
