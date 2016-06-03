using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Interfaced;

namespace Manual
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var demos = GetDemoList();

            if (args.Length < 1)
            {
                ShowUsage(demos);
                return 1;
            }

            var command = args[0];
            foreach (var demo in demos)
            {
                if (string.Compare(demo.Name.Substring(4), command, ignoreCase: true) == 0)
                {
                    DemoAsync(demo, args.Skip(1).ToArray()).GetAwaiter().GetResult();
                    return 0;
                }
            }

            ShowUsage(demos);
            return 1;
        }

        private static List<Type> GetDemoList()
        {
            return typeof(Program).Assembly.GetTypes().Where(t => t.Name.StartsWith("Demo")).ToList();
        }

        private static async Task DemoAsync(Type demo, string[] args)
        {
            Console.WriteLine($"{demo.Name}");
            Console.WriteLine(new string('=', 79));
            Console.WriteLine();

            var methods = demo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .Where(m => m.Name.StartsWith("Demo") && m.GetParameters().Length == 0);

            foreach (var method in methods)
            {
                var orgColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("* " + method.Name);
                Console.ForegroundColor = orgColor;

                var system = ActorSystem.Create("Demo", "akka.loglevel=off \n akka.suppress-json-serializer-warning=on");
                DeadRequestProcessingActor.Install(system);

                var demoInstance = Activator.CreateInstance(demo, system, args);
                var result = method.Invoke(demoInstance, new object[0]);
                if (result is Task)
                    await (Task)result;

                Console.WriteLine();
                await system.Terminate();
            }
        }

        private static void ShowUsage(List<Type> demos)
        {
            Console.WriteLine("usage: program.exe <command>");
            Console.WriteLine("command:");
            foreach (var demo in demos)
            {
                Console.WriteLine("  " + demo.Name.Substring(4));
            }
        }
    }
}
