using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Xunit;
using Xunit.Abstractions;

namespace CodeGenerator
{
    public interface IGreeter : IInterfacedActor
    {
        Task<string> Greet(string name);
        Task<int> GetCount();
    }

    public class InterfacedActorCodeGeneratorTest
    {
        private readonly ITestOutputHelper _output;

        public InterfacedActorCodeGeneratorTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GenerateActorCode()
        {
            var compilation = TestUtility.Generate(new Options(), new[] { typeof(IGreeter) }, _output);
            Assert.Equal(
                new[]
                {
                    "IGreeter_PayloadTable",
                    "GetCount_Invoke",
                    "GetCount_Return",
                    "Greet_Invoke",
                    "Greet_Return",
                    "IGreeter_NoReply",
                    "GreeterRef",
                    "IGreeterSync",
                },
                compilation.GetTypeSymbolNames());
        }

        [Fact]
        public void GenerateActorCode_WithUseSlimClient()
        {
            var compilation = TestUtility.Generate(new Options { UseSlimClient = true }, new[] { typeof(IGreeter) }, _output);
            Assert.Equal(
                new[]
                {
                    "IGreeter_PayloadTable",
                    "GetCount_Invoke",
                    "GetCount_Return",
                    "Greet_Invoke",
                    "Greet_Return",
                    "IGreeter_NoReply",
                    "GreeterRef",
                },
                compilation.GetTypeSymbolNames());
        }
    }
}
