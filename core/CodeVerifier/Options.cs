using CommandLine;

namespace CodeVerifier
{
    internal class Options
    {
        [Value(0, Required = true, HelpText = "Input assembly path")]
        public string Input { get; set; }

        [Option]
        public bool Verbose { get; set; }
    }
}
