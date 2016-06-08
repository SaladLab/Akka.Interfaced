using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class GracefulShutdownTest : TestKit.Xunit2.TestKit
    {
        public GracefulShutdownTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestGracefulShutdownActor : InterfacedActor, IWorker
        {
            private readonly LogBoard<string> _log;

            public TestGracefulShutdownActor(LogBoard<string> log)
            {
                _log = log;
            }

            protected override async Task OnGracefulStop()
            {
                _log.Add("OnGracefulStop");
                await Task.Delay(10);
                _log.Add("OnGracefulStop done");
            }

            async Task IWorker.Atomic(int id)
            {
                _log.Add($"Atomic({id})");
                await Task.Delay(10);
                _log.Add($"Atomic({id}) done");
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _log.Add($"Reentrant({id})");
                await Task.Delay(10);
                _log.Add($"Reentrant({id}) done");
            }
        }

        [Fact]
        public async Task GetInterfacedPoisonPill_WaitForAllAtomicHandlersDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new WorkerRef(ActorOf(() => new TestGracefulShutdownActor(log)));

            // Act
            a.WithNoReply().Atomic(1);
            a.WithNoReply().Atomic(2);
            await a.Actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(new List<string>
            {
                "Atomic(1)",
                "Atomic(1) done",
                "Atomic(2)",
                "Atomic(2) done",
                "OnGracefulStop",
                "OnGracefulStop done"
            }, log);
        }

        [Fact]
        public async Task GetInterfacedPoisonPill_WaitForAllReentrantHandlersDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new WorkerRef(ActorOf(() => new TestGracefulShutdownActor(log)));

            // Act
            a.WithNoReply().Reentrant(1);
            a.WithNoReply().Reentrant(2);
            await a.Actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.True(new HashSet<string>
            {
                "Reentrant(1)",
                "Reentrant(1) done",
                "Reentrant(2)",
                "Reentrant(2) done",
            }.SetEquals(log.Take(4)));
            Assert.Equal(new List<string>
            {
                "OnGracefulStop",
                "OnGracefulStop done"
            }, log.Skip(4));
        }
    }
}
