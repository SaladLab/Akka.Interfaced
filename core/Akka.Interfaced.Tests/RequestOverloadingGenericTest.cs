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

        public class TestOverloadedActor : InterfacedActor, IOverloadedGeneric
        {
            private LogBoard<string> _log;

            public TestOverloadedActor(LogBoard<string> log)
            {
                _log = log;
            }

            private static T Min<T>(T a, T b) => Comparer<T>.Default.Compare(a, b) > 0 ? a : b;

            Task<T> IOverloadedGeneric.Min<T>(T a, T b)
            {
                _log.Add($"Min({a}, {b})");
                return Task.FromResult(Min(a, b));
            }

            Task<T> IOverloadedGeneric.Min<T>(T a, T b, T c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Task.FromResult(Min(Min(a, b), c));
            }

            Task<T> IOverloadedGeneric.Min<T>(params T[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return Task.FromResult(nums.Aggregate((a, b) => Min(a, b)));
            }
        }

        /*
        [Theory]
        [InlineData(typeof(TestOverloadedActor))]
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
        */
    }
}
