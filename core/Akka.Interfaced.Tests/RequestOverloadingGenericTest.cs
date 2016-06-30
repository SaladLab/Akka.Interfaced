using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestOverloadingGenericTest : TestKit.Xunit2.TestKit
    {
        public RequestOverloadingGenericTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public static class Math
        {
            public static T Min<T>(T a, T b) => Comparer<T>.Default.Compare(a, b) < 0 ? a : b;
        }

        public class TestOverloadedActor : InterfacedActor, IOverloadedGeneric
        {
            private LogBoard<string> _log;

            public TestOverloadedActor(LogBoard<string> log)
            {
                _log = log;
            }

            Task<T> IOverloadedGeneric.Min<T>(T a, T b)
            {
                _log.Add($"Min({a}, {b})");
                return Task.FromResult(Math.Min(a, b));
            }

            Task<T> IOverloadedGeneric.Min<T>(T a, T b, T c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Task.FromResult(Math.Min(Math.Min(a, b), c));
            }

            Task<T> IOverloadedGeneric.Min<T>(params T[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return Task.FromResult(nums.Min());
            }
        }

        public class TestOverloadedSyncActor : InterfacedActor, IOverloadedGenericSync
        {
            private LogBoard<string> _log;

            public TestOverloadedSyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            T IOverloadedGenericSync.Min<T>(T a, T b)
            {
                _log.Add($"Min({a}, {b})");
                return Math.Min(a, b);
            }

            T IOverloadedGenericSync.Min<T>(T a, T b, T c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Math.Min(Math.Min(a, b), c);
            }

            T IOverloadedGenericSync.Min<T>(params T[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return nums.Min();
            }
        }

        public class TestOverloadedExtendedActor : InterfacedActor, IExtendedInterface<IOverloadedGeneric>
        {
            private LogBoard<string> _log;

            public TestOverloadedExtendedActor(LogBoard<string> log)
            {
                _log = log;
            }

            [ExtendedHandler]
            private T Min<T>(T a, T b)
            {
                _log.Add($"Min({a}, {b})");
                return Math.Min(a, b);
            }

            [ExtendedHandler]
            private T Min<T>(T a, T b, T c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Math.Min(Math.Min(a, b), c);
            }

            [ExtendedHandler]
            private Task<T> Min<T>(params T[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return Task.FromResult(nums.Min());
            }
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor))]
        [InlineData(typeof(TestOverloadedSyncActor))]
        [InlineData(typeof(TestOverloadedExtendedActor))]
        public async Task Min2_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedGenericRef>();

            // Act
            var ret = await a.Min(1, 2);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2)" }, log);
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor))]
        [InlineData(typeof(TestOverloadedSyncActor))]
        [InlineData(typeof(TestOverloadedExtendedActor))]
        public async Task Min3_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedGenericRef>();

            // Act
            var ret = await a.Min(1, 2, 3);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2, 3)" }, log);
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor))]
        [InlineData(typeof(TestOverloadedSyncActor))]
        [InlineData(typeof(TestOverloadedExtendedActor))]
        public async Task Mins_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedGenericRef>();

            // Act
            var ret = await a.Min(1, 2, 3, 4);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2, 3, 4)" }, log);
        }

        public class TestOverloadedActor<T> : InterfacedActor, IOverloadedGeneric<T>
            where T : new()
        {
            private LogBoard<string> _log;

            public TestOverloadedActor(LogBoard<string> log)
            {
                _log = log;
            }

            Task<T> IOverloadedGeneric<T>.GetDefault()
            {
                return Task.FromResult(default(T));
            }

            Task<U> IOverloadedGeneric<T>.Min<U>(U a, U b)
            {
                _log.Add($"Min({a}, {b})");
                return Task.FromResult(Math.Min(a, b));
            }

            Task<U> IOverloadedGeneric<T>.Min<U>(U a, U b, U c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Task.FromResult(Math.Min(Math.Min(a, b), c));
            }

            Task<U> IOverloadedGeneric<T>.Min<U>(params U[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return Task.FromResult(nums.Min());
            }
        }

        public class TestOverloadedSyncActor<T> : InterfacedActor, IOverloadedGenericSync<T>
            where T : new()
        {
            private LogBoard<string> _log;

            public TestOverloadedSyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            T IOverloadedGenericSync<T>.GetDefault()
            {
                return default(T);
            }

            U IOverloadedGenericSync<T>.Min<U>(U a, U b)
            {
                _log.Add($"Min({a}, {b})");
                return Math.Min(a, b);
            }

            U IOverloadedGenericSync<T>.Min<U>(U a, U b, U c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Math.Min(Math.Min(a, b), c);
            }

            U IOverloadedGenericSync<T>.Min<U>(params U[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return nums.Min();
            }
        }

        public class TestOverloadedExtendedActor<T> : InterfacedActor, IExtendedInterface<IOverloadedGeneric<T>>
            where T : new()
        {
            private LogBoard<string> _log;

            public TestOverloadedExtendedActor(LogBoard<string> log)
            {
                _log = log;
            }

            [ExtendedHandler]
            private T GetDefault()
            {
                return default(T);
            }

            [ExtendedHandler]
            private U Min<U>(U a, U b)
            {
                _log.Add($"Min({a}, {b})");
                return Math.Min(a, b);
            }

            [ExtendedHandler]
            private U Min<U>(U a, U b, U c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Math.Min(Math.Min(a, b), c);
            }

            [ExtendedHandler]
            private Task<U> Min<U>(params U[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return Task.FromResult(nums.Min());
            }
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor<double>))]
        [InlineData(typeof(TestOverloadedSyncActor<double>))]
        [InlineData(typeof(TestOverloadedExtendedActor<double>))]
        public async Task GenericInterface_Min2_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedGenericRef<double>>();

            // Act
            var ret = await a.Min(1, 2);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2)" }, log);
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor<double>))]
        [InlineData(typeof(TestOverloadedSyncActor<double>))]
        [InlineData(typeof(TestOverloadedExtendedActor<double>))]
        public async Task GenericInterface_Min3_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedGenericRef<double>>();

            // Act
            var ret = await a.Min(1, 2, 3);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2, 3)" }, log);
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor<double>))]
        [InlineData(typeof(TestOverloadedSyncActor<double>))]
        [InlineData(typeof(TestOverloadedExtendedActor<double>))]
        public async Task GenericInterface_Mins_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedGenericRef<double>>();

            // Act
            var ret = await a.Min(1, 2, 3, 4);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2, 3, 4)" }, log);
        }
    }
}
