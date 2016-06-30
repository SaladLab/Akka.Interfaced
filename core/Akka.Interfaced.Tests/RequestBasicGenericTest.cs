using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestBasicGenericTest : TestKit.Xunit2.TestKit
    {
        public RequestBasicGenericTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        private static class IdentityHelper<T>
        {
            public static T Identity
            {
                get
                {
                    var type = typeof(T);
                    if (type == typeof(bool))
                        return (T)(object)true;
                    if (type == typeof(int))
                        return (T)(object)1;
                    if (type == typeof(double))
                        return (T)(object)1.0;
                    throw new ArgumentException(nameof(T));
                }
            }
        }

        private static class ThrowExceptionHelper
        {
            public static void Throw(ThrowExceptionType type)
            {
                switch (type)
                {
                    case ThrowExceptionType.ResponsiveByWrap:
                        throw new ResponsiveException(new ArgumentException(nameof(type)));

                    case ThrowExceptionType.ResponsiveByFilter:
                        throw new ArgumentException(nameof(type));

                    case ThrowExceptionType.Fault:
                        throw new InvalidOperationException(nameof(type));
                }
            }
        }

        public class TestBasicActor : InterfacedActor, IBasic<int>
        {
            private LogBoard<string> _log;

            public TestBasicActor(LogBoard<string> log)
            {
                _log = log;
            }

            Task IBasic<int>.Call()
            {
                _log.Add($"Call()");
                return Task.FromResult(0);
            }

            Task IBasic<int>.CallWithParameter(int value)
            {
                _log.Add($"CallWithParameter({value})");
                return Task.FromResult(value);
            }

            Task<int> IBasic<int>.CallWithParameterAndReturn(int value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return Task.FromResult(value);
            }

            Task<int> IBasic<int>.CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return Task.FromResult(1);
            }

            [ResponsiveException(typeof(ArgumentException))]
            Task<int> IBasic<int>.ThrowException(ThrowExceptionType type)
            {
                _log.Add($"ThrowException({type})");
                ThrowExceptionHelper.Throw(type);
                return Task.FromResult(1);
            }
        }

        public class TestBasicSyncActor : InterfacedActor, IBasicSync<int>
        {
            private LogBoard<string> _log;

            public TestBasicSyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            void IBasicSync<int>.Call()
            {
                _log.Add($"Call()");
            }

            void IBasicSync<int>.CallWithParameter(int value)
            {
                _log.Add($"CallWithParameter({value})");
            }

            int IBasicSync<int>.CallWithParameterAndReturn(int value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return value;
            }

            int IBasicSync<int>.CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return 1;
            }

            [ResponsiveException(typeof(ArgumentException))]
            int IBasicSync<int>.ThrowException(ThrowExceptionType type)
            {
                _log.Add($"ThrowException({type})");
                ThrowExceptionHelper.Throw(type);
                return 1;
            }
        }

        public class TestBasicExtendedActor : InterfacedActor, IExtendedInterface<IBasic<int>>
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
            private Task<int> ThrowException(ThrowExceptionType type)
            {
                _log.Add($"ThrowException({type})");
                ThrowExceptionHelper.Throw(type);
                return Task.FromResult(1);
            }
        }

        public class TestBasicActor<T> : InterfacedActor, IBasic<T>
            where T : new()
        {
            private LogBoard<string> _log;

            public TestBasicActor(LogBoard<string> log)
            {
                _log = log;
            }

            Task IBasic<T>.Call()
            {
                _log.Add($"Call()");
                return Task.FromResult(true);
            }

            Task IBasic<T>.CallWithParameter(T value)
            {
                _log.Add($"CallWithParameter({value})");
                return Task.FromResult(true);
            }

            Task<T> IBasic<T>.CallWithParameterAndReturn(T value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return Task.FromResult(value);
            }

            Task<T> IBasic<T>.CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return Task.FromResult(IdentityHelper<T>.Identity);
            }

            [ResponsiveException(typeof(ArgumentException))]
            Task<T> IBasic<T>.ThrowException(ThrowExceptionType type)
            {
                _log.Add($"ThrowException({type})");
                ThrowExceptionHelper.Throw(type);
                return Task.FromResult(IdentityHelper<T>.Identity);
            }
        }

        public class TestBasicSyncActor<T> : InterfacedActor, IBasicSync<T>
            where T : new()
        {
            private LogBoard<string> _log;

            public TestBasicSyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            void IBasicSync<T>.Call()
            {
                _log.Add($"Call()");
            }

            void IBasicSync<T>.CallWithParameter(T value)
            {
                _log.Add($"CallWithParameter({value})");
            }

            T IBasicSync<T>.CallWithParameterAndReturn(T value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return value;
            }

            T IBasicSync<T>.CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return IdentityHelper<T>.Identity;
            }

            [ResponsiveException(typeof(ArgumentException))]
            T IBasicSync<T>.ThrowException(ThrowExceptionType type)
            {
                _log.Add($"ThrowException({type})");
                ThrowExceptionHelper.Throw(type);
                return IdentityHelper<T>.Identity;
            }
        }

        public class TestBasicExtendedActor<T> : InterfacedActor, IExtendedInterface<IBasic<T>>
            where T : new()
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
            private void CallWithParameter(T value)
            {
                _log.Add($"CallWithParameter({value})");
            }

            [ExtendedHandler]
            private T CallWithParameterAndReturn(T value)
            {
                _log.Add($"CallWithParameterAndReturn({value})");
                return value;
            }

            [ExtendedHandler]
            private Task<T> CallWithReturn()
            {
                _log.Add($"CallWithReturn()");
                return Task.FromResult(IdentityHelper<T>.Identity);
            }

            [ExtendedHandler]
            [ResponsiveException(typeof(ArgumentException))]
            private Task<T> ThrowException(ThrowExceptionType type)
            {
                _log.Add($"ThrowException({type})");
                ThrowExceptionHelper.Throw(type);
                return Task.FromResult(IdentityHelper<T>.Identity);
            }
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        [InlineData(typeof(TestBasicActor<int>))]
        [InlineData(typeof(TestBasicSyncActor<int>))]
        [InlineData(typeof(TestBasicExtendedActor<int>))]
        public async Task BasicCall_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<BasicRef<int>>();

            // Act
            await a.Call();

            // Assert
            Assert.Equal(new[] { "Call()" }, log);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        [InlineData(typeof(TestBasicActor<int>))]
        [InlineData(typeof(TestBasicSyncActor<int>))]
        [InlineData(typeof(TestBasicExtendedActor<int>))]
        public async Task BasicCallWithParameter_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<BasicRef<int>>();

            // Act
            await a.CallWithParameter(1);

            // Assert
            Assert.Equal(new[] { "CallWithParameter(1)" }, log);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        [InlineData(typeof(TestBasicActor<int>))]
        [InlineData(typeof(TestBasicSyncActor<int>))]
        [InlineData(typeof(TestBasicExtendedActor<int>))]
        public async Task BasicCallWithParameterAndReturn_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<BasicRef<int>>();

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
        [InlineData(typeof(TestBasicActor<int>))]
        [InlineData(typeof(TestBasicSyncActor<int>))]
        [InlineData(typeof(TestBasicExtendedActor<int>))]
        public async Task BasicCallWithReturn_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<BasicRef<int>>();

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
        [InlineData(typeof(TestBasicActor<int>))]
        [InlineData(typeof(TestBasicSyncActor<int>))]
        [InlineData(typeof(TestBasicExtendedActor<int>))]
        public async Task BasicThrowExceptionWithWrap_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<BasicRef<int>>();

            // Act
            var e = await Record.ExceptionAsync(() => a.ThrowException(ThrowExceptionType.ResponsiveByWrap));

            // Assert
            Assert.Equal(new[] { "ThrowException(ResponsiveByWrap)" }, log);
            Assert.IsType<ArgumentException>(e);
        }

        [Theory]
        [InlineData(typeof(TestBasicActor))]
        [InlineData(typeof(TestBasicSyncActor))]
        [InlineData(typeof(TestBasicExtendedActor))]
        [InlineData(typeof(TestBasicActor<int>))]
        [InlineData(typeof(TestBasicSyncActor<int>))]
        [InlineData(typeof(TestBasicExtendedActor<int>))]
        public async Task BasicThrowExceptionWithFilter_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<BasicRef<int>>();

            // Act
            var e = await Record.ExceptionAsync(() => a.ThrowException(ThrowExceptionType.ResponsiveByFilter));

            // Assert
            Assert.Equal(new[] { "ThrowException(ResponsiveByFilter)" }, log);
            Assert.IsType<ArgumentException>(e);
        }
    }
}
