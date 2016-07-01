using System;
using Akka.Interfaced;
using Xunit;
using Xunit.Abstractions;

namespace CodeGenerator
{
    public interface IGreetObserver : IInterfacedObserver
    {
        void Event(string message);
    }

    public interface IGreetObserver<T> : IInterfacedObserver
        where T : new()
    {
        void Event(T message);
        void Event<U>(T message, U param)
            where U : IEquatable<U>;
    }

    public class InterfacedObserverCodeGeneratorTest
    {
        private readonly ITestOutputHelper _output;

        public InterfacedObserverCodeGeneratorTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GenerateObserverCode()
        {
            var compilation = TestUtility.Generate(new Options(), new[] { typeof(IGreetObserver) }, _output);
            Assert.Equal(
                new[]
                {
                    "IGreetObserver_PayloadTable",
                    "Event_Invoke",
                    "GreetObserver",
                    "IGreetObserverAsync",
                },
                compilation.GetTypeSymbolNames());
        }

        [Fact]
        public void GenerateObserverCode_WithUseSlimClient()
        {
            var compilation = TestUtility.Generate(new Options { UseSlimClient = true }, new[] { typeof(IGreetObserver) }, _output);
            Assert.Equal(
                new[]
                {
                    "IGreetObserver_PayloadTable",
                    "Event_Invoke",
                    "GreetObserver",
                },
                compilation.GetTypeSymbolNames());
        }

        [Fact]
        public void GenerateObserverCode_WithGeneric()
        {
            var compilation = TestUtility.Generate(new Options { UseSlimClient = true }, new[] { typeof(IGreetObserver<>) }, _output);
            Assert.Equal(
                new[]
                {
                    "IGreetObserver_PayloadTable",
                    "Event_Invoke",
                    "Event_2_Invoke",
                    "GreetObserver",
                },
                compilation.GetTypeSymbolNames());
        }
    }
}
