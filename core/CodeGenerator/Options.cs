using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace CodeGenerator
{
    public class Options
    {
        [Option('p', "path", HelpText = "Base path for processing sources, references and target.")]
        public string Path { get; set; }

        [Option('s', "source", Separator = ';', HelpText = "Input source files.")]
        public IEnumerable<string> Sources { get; set; }

        [Option('r', "reference", Separator = ';', HelpText = "Input reference files for building sources.")]
        public IEnumerable<string> References { get; set; }

        [Option('d', "define", Separator = ';', HelpText = "Defines name as a symbol which is used in compiling.")]
        public IEnumerable<string> Defines { get; set; }

        [Option('t', "target", HelpText = "Filename of a generated code.")]
        public string TargetFile { get; set; }

        [Option('s', "slimclient", HelpText = "Use slim-client code generation instead of full one.")]
        public bool UseSlimClient { get; set; }

        [Option('c', "protobuf", HelpText = "Use protobuf-net for serializing generated message type.")]
        public bool UseProtobuf { get; set; }

        [Option("include", Separator = ';', HelpText = "Source include regular expression filter.")]
        public IEnumerable<string> Includes { get; set; }

        [Option("exclude", Separator = ';', HelpText = "Source exclude regular expression filter.")]
        public IEnumerable<string> Excludes { get; set; }
    }
}
