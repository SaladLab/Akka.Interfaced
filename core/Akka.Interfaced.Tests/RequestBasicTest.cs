using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestBasicTest : TestKit.Xunit2.TestKit
    {
        public RequestBasicTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestBasicActor : InterfacedActor, IBasic
        {
            private LogBoard<string> _logBoard;

            public TestBasicActor(LogBoard<string> logBoard)
            {
                _logBoard = logBoard;
            }

            Task IBasic.Call()
            {
                _logBoard.Log($"Call()");
                return Task.FromResult(0);
            }

            Task IBasic.CallWithParameter(int value)
            {
                _logBoard.Log($"CallWithParameter({value})");
                return Task.FromResult(0);
            }

            Task<int> IBasic.CallWithParameterAndReturn(int value)
            {
                _logBoard.Log($"CallWithParameterAndReturn({value})");
                return Task.FromResult(value);
            }

            Task<int> IBasic.CallWithReturn()
            {
                _logBoard.Log($"CallWithReturn()");
                return Task.FromResult(1);
            }

            [ResponsiveException(typeof(ArgumentException))]
            Task<int> IBasic.ThrowException(bool throwException)
            {
                _logBoard.Log($"ThrowException({throwException})");

                if (throwException)
                    throw new ArgumentException("throwException");

                return Task.FromResult(1);
            }
        }

        public class TestBasicSyncActor : InterfacedActor, IBasicSync
        {
            private LogBoard<string> _logBoard;

            public TestBasicSyncActor(LogBoard<string> logBoard)
            {
                _logBoard = logBoard;
            }

            void IBasicSync.Call()
            {
                _logBoard.Log($"Call()");
            }

            void IBasicSync.CallWithParameter(int value)
            {
                _logBoard.Log($"CallWithParameter({value})");
            }

            int IBasicSync.CallWithParameterAndReturn(int value)
            {
                _logBoard.Log($"CallWithParameterAndReturn({value})");
                return value;
            }

            int IBasicSync.CallWithReturn()
            {
                _logBoard.Log($"CallWithReturn()");
                return 1;
            }

            [ResponsiveException(typeof(ArgumentException))]
            int IBasicSync.ThrowException(bool throwException)
            {
                _logBoard.Log($"ThrowException({throwException})");

                if (throwException)
                    throw new ArgumentException("throwException");

                return 1;
            }
        }

        public class TestBasicExtendedActor : InterfacedActor, IExtendedInterface<IBasic>
        {
            private LogBoard<string> _logBoard;

            public TestBasicExtendedActor(LogBoard<string> logBoard)
            {
                _logBoard = logBoard;
            }

            [ExtendedHandler]
            private void Call()
            {
                _logBoard.Log($"Call()");
            }

            [ExtendedHandler]
            private void CallWithParameter(int value)
            {
                _logBoard.Log($"CallWithParameter({value})");
            }

            [ExtendedHandler]
            private int CallWithParameterAndReturn(int value)
            {
                _logBoard.Log($"CallWithParameterAndReturn({value})");
                return value;
            }

            [ExtendedHandler]
            private Task<int> CallWithReturn()
            {
                _logBoard.Log($"CallWithReturn()");
                return Task.FromResult(1);
            }

            [ExtendedHandler]
            [ResponsiveException(typeof(ArgumentException))]
            private Task<int> ThrowException(bool throwException)
            {
                _logBoard.Log($"ThrowException({throwException})");

                if (throwException)
                    throw new ArgumentException("throwException");

                return Task.FromResult(1);
            }
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCall_Done(Type actorType)
        {
            // Arrange
            var board = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, board)));

            // Act
            await a.Call();

            // Assert
            Assert.Equal(new[] { "Call()" }, board.GetLogs());
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCallWithParameter_Done(Type actorType)
        {
            // Arrange
            var board = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, board)));

            // Act
            await a.CallWithParameter(1);

            // Assert
            Assert.Equal(new[] { "CallWithParameter(1)" }, board.GetLogs());
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCallWithParameterAndReturn_Done(Type actorType)
        {
            // Arrange
            var board = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, board)));

            // Act
            var r = await a.CallWithParameterAndReturn(1);

            // Assert
            Assert.Equal(new[] { "CallWithParameterAndReturn(1)" }, board.GetLogs());
            Assert.Equal(1, r);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCallWithReturn_Done(Type actorType)
        {
            // Arrange
            var board = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, board)));

            // Act
            var r = await a.CallWithReturn();

            // Assert
            Assert.Equal(new[] { "CallWithReturn()" }, board.GetLogs());
            Assert.Equal(1, r);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicThrowException_Done(Type actorType)
        {
            // Arrange
            var board = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, board)));

            // Act
            var e = await Record.ExceptionAsync(() => a.ThrowException(true));

            // Assert
            Assert.Equal(new[] { "ThrowException(True)" }, board.GetLogs());
            Assert.IsType<ArgumentException>(e);
        }
    }
}
