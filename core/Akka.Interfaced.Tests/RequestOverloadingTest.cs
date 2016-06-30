using System;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestOverloadingTest : TestKit.Xunit2.TestKit
    {
        public RequestOverloadingTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestOverloadedActor : InterfacedActor, IOverloaded
        {
            private LogBoard<string> _log;

            public TestOverloadedActor(LogBoard<string> log)
            {
                _log = log;
            }

            Task<int> IOverloaded.Min(int a, int b)
            {
                _log.Add($"Min({a}, {b})");
                return Task.FromResult(Math.Min(a, b));
            }

            Task<int> IOverloaded.Min(int a, int b, int c)
            {
                _log.Add($"Min({a}, {b}, {c})");
                return Task.FromResult(Math.Min(Math.Min(a, b), c));
            }

            Task<int> IOverloaded.Min(params int[] nums)
            {
                _log.Add($"Min({string.Join(", ", nums)})");
                return Task.FromResult(nums.Min());
            }
        }

        [Theory]
        [InlineData(typeof(TestOverloadedActor))]
        public async Task Min2_Done(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedRef>();

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
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedRef>();

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
            var a = ActorOf(Props.Create(actorType, log)).Cast<OverloadedRef>();

            // Act
            var ret = await a.Min(1, 2, 3, 4);

            // Assert
            Assert.Equal(1, ret);
            Assert.Equal(new[] { "Min(1, 2, 3, 4)" }, log);
        }
    }
}
