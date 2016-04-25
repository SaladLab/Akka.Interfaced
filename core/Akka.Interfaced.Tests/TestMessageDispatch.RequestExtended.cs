using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    public class TestMessageDispatchRequestExtendedActor : InterfacedActor<TestMessageDispatchRequestExtendedActor>,
        IDummy,
        IExtendedInterface<IBasic>
    {
        private List<string> _eventLog;

        public TestMessageDispatchRequestExtendedActor(List<string> eventLog)
        {
            _eventLog = eventLog;
        }

        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }

        [ExtendedHandler]
        private Task Call()
        {
            _eventLog.Add("Call");
            return Task.FromResult(0);
        }

        [ExtendedHandler]
        private Task CallWithParameter(int value)
        {
            _eventLog.Add($"CallWithParameter({value})");
            return Task.FromResult(0);
        }

        [ExtendedHandler]
        private Task<int> CallWithReturn()
        {
            _eventLog.Add("CallWithReturn");
            return Task.FromResult(1);
        }

        [ExtendedHandler]
        private Task<int> CallWithParameterAndReturn(int value)
        {
            _eventLog.Add($"CallWithParameterAndReturn({value})");
            return Task.FromResult(value);
        }

        [ExtendedHandler]
        private Task<int> ThrowException(bool throwException)
        {
            _eventLog.Add($"ThrowException({throwException})");
            if (throwException)
                throw new ArgumentException("throwException");
            return Task.FromResult(0);
        }
    }

    public class TestMessageDispatchRequestExtendedActor2 : InterfacedActor<TestMessageDispatchRequestExtendedActor2>,
           IDummy,
           IExtendedInterface<IBasic>
    {
        private readonly List<string> _eventLog;

        public TestMessageDispatchRequestExtendedActor2(List<string> eventLog)
        {
            _eventLog = eventLog;
        }

        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }

        [ExtendedHandler]
        private void Call()
        {
            _eventLog.Add("Call");
        }

        [ExtendedHandler]
        private void CallWithParameter(int value)
        {
            _eventLog.Add($"CallWithParameter({value})");
        }

        [ExtendedHandler]
        private int CallWithReturn()
        {
            _eventLog.Add("CallWithReturn");
            return 1;
        }

        [ExtendedHandler]
        private int CallWithParameterAndReturn(int value)
        {
            _eventLog.Add($"CallWithParameterAndReturn({value})");
            return value;
        }

        [ExtendedHandler]
        private int ThrowException(bool throwException)
        {
            _eventLog.Add($"ThrowException({throwException})");
            if (throwException)
                throw new ArgumentException("throwException");
            return 0;
        }
    }

    public class TestMessageDispatchRequestExtended : Akka.TestKit.Xunit2.TestKit
    {
        private Tuple<BasicRef, List<string>> CreateActorAndEventLog()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestMessageDispatchRequestExtendedActor>(
                Props.Create<TestMessageDispatchRequestExtendedActor>(eventLog));
            return Tuple.Create(new BasicRef(actor), eventLog);
        }

        public TestMessageDispatchRequestExtended(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_can_handle_call()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            await a.Item1.Call();

            // Assert
            Assert.Equal(new List<string> { "Call" }, a.Item2);
        }

        [Fact]
        public async Task Test_can_handle_call_with_parameter()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            await a.Item1.CallWithParameter(100);

            // Assert
            Assert.Equal(new List<string> { "CallWithParameter(100)" }, a.Item2);
        }

        [Fact]
        public async Task Test_can_handle_call_with_return()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            var r = await a.Item1.CallWithReturn();

            // Assert
            Assert.Equal(new List<string> { "CallWithReturn" }, a.Item2);
            Assert.Equal(1, r);
        }

        [Fact]
        public async Task Test_can_handle_call_with_parameter_and_return()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            var r = await a.Item1.CallWithParameterAndReturn(100);

            // Assert
            Assert.Equal(new List<string> { "CallWithParameterAndReturn(100)" }, a.Item2);
            Assert.Equal(100, r);
        }

        [Fact]
        public async Task Test_can_handle_plain_message()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await a.Item1.ThrowException(true);
            });
        }
    }

    public class TestMessageDispatchRequestExtended2 : Akka.TestKit.Xunit2.TestKit
    {
        private Tuple<BasicRef, List<string>> CreateActorAndEventLog()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestMessageDispatchRequestExtendedActor2>(
                Props.Create<TestMessageDispatchRequestExtendedActor2>(eventLog));
            return Tuple.Create(new BasicRef(actor), eventLog);
        }

        public TestMessageDispatchRequestExtended2(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_can_handle_call()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            await a.Item1.Call();

            // Assert
            Assert.Equal(new List<string> { "Call" }, a.Item2);
        }

        [Fact]
        public async Task Test_can_handle_call_with_parameter()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            await a.Item1.CallWithParameter(100);

            // Assert
            Assert.Equal(new List<string> { "CallWithParameter(100)" }, a.Item2);
        }

        [Fact]
        public async Task Test_can_handle_call_with_return()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            var r = await a.Item1.CallWithReturn();

            // Assert
            Assert.Equal(new List<string> { "CallWithReturn" }, a.Item2);
            Assert.Equal(1, r);
        }

        [Fact]
        public async Task Test_can_handle_call_with_parameter_and_return()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act
            var r = await a.Item1.CallWithParameterAndReturn(100);

            // Assert
            Assert.Equal(new List<string> { "CallWithParameterAndReturn(100)" }, a.Item2);
            Assert.Equal(100, r);
        }

        [Fact]
        public async Task Test_can_handle_plain_message()
        {
            // Arrange
            var a = CreateActorAndEventLog();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await a.Item1.ThrowException(true);
            });
        }
    }
}
