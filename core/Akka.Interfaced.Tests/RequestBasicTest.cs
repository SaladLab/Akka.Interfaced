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
            private LogBoard<string> _log;

            public TestBasicActor(LogBoard<string> log)
            {
                _log = log;
            }

            Task IBasic.Call()
            {
                _log.Add($"Call()");
                return Task.FromResult(0);
            }

            Task IBasic.CallWithParameter(int value)
            {
                _log.Add($"CallWithParameter({value})");
                return Task.FromResult(0);
            }

            Task<int> IBasic.CallWithParameterAndReturn(int value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return Task.FromResult(value);
            }

            Task<int> IBasic.CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return Task.FromResult(1);
            }

            [ResponsiveException(typeof(ArgumentException))]
            Task<int> IBasic.ThrowException(bool throwException)
            {
                _log.Add($"ThrowException({throwException})");

                if (throwException)
                    throw new ArgumentException("throwException");

                return Task.FromResult(1);
            }
        }

        public class TestBasicSyncActor : InterfacedActor, IBasicSync
        {
            private LogBoard<string> _log;

            public TestBasicSyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            void IBasicSync.Call()
            {
                _log.Add($"Call()");
            }

            void IBasicSync.CallWithParameter(int value)
            {
                _log.Add($"CallWithParameter({value})");
            }

            int IBasicSync.CallWithParameterAndReturn(int value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return value;
            }

            int IBasicSync.CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return 1;
            }

            [ResponsiveException(typeof(ArgumentException))]
            int IBasicSync.ThrowException(bool throwException)
            {
                _log.Add($"ThrowException({throwException})");

                if (throwException)
                    throw new ArgumentException("throwException");

                return 1;
            }
        }

        public class TestBasicExtendedActor : InterfacedActor, IExtendedInterface<IBasic>
        {
            private LogBoard<string> _log;

            public TestBasicExtendedActor(LogBoard<string> log)
            {
                _log = log;
            }

            [ExtendedHandler]
            private void Call()
            {
                _log.Add($"Call()");
            }

            [ExtendedHandler]
            private void CallWithParameter(int value)
            {
                _log.Add($"CallWithParameter({value})");
            }

            [ExtendedHandler]
            private int CallWithParameterAndReturn(int value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return value;
            }

            [ExtendedHandler]
            private Task<int> CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return Task.FromResult(1);
            }

            [ExtendedHandler]
            [ResponsiveException(typeof(ArgumentException))]
            private Task<int> ThrowException(bool throwException)
            {
                _log.Add($"ThrowException({throwException})");

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
            var log = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, log)));

            // Act
            await a.Call();

            // Assert
            Assert.Equal(new[] { "Call()" }, log);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCallWithParameter_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, log)));

            // Act
            await a.CallWithParameter(1);

            // Assert
            Assert.Equal(new[] { "CallWithParameter(1)" }, log);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCallWithParameterAndReturn_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, log)));

            // Act
            var r = await a.CallWithParameterAndReturn(1);

            // Assert
            Assert.Equal(new[] { "CallWithParameterAndReturn(1)" }, log);
            Assert.Equal(1, r);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicCallWithReturn_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, log)));

            // Act
            var r = await a.CallWithReturn();

            // Assert
            Assert.Equal(new[] { "CallWithReturn()" }, log);
            Assert.Equal(1, r);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        public async Task BasicThrowException_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new BasicRef(ActorOf(Props.Create(actorType, log)));

            // Act
            var e = await Record.ExceptionAsync(() => a.ThrowException(true));

            // Assert
            Assert.Equal(new[] { "ThrowException(True)" }, log);
            Assert.IsType<ArgumentException>(e);
        }
    }
}
