using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class TaskRunTest : TestKit.Xunit2.TestKit
    {
        public TaskRunTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestRunActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestRunActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void Handle(string message)
            {
                var type = message[0];
                var value = message.Length >= 2 ? message.Substring(2) : "";
                switch (type)
                {
                    case 'S':
                        RunTask(() =>
                        {
                            _log.Add(message);
                        });
                        break;

                    case 'A':
                        RunTask(async () =>
                        {
                            _log.Add(message);
                            await Task.Delay(10);
                            _log.Add(message + " done");
                        });
                        break;

                    case 'R':
                        RunTask(async () =>
                        {
                            _log.Add(message);
                            await Task.Delay(10);
                            _log.Add(message + " done");
                        },
                        isReentrant: true);
                        break;

                    case 'X':
                        Self.Tell(InterfacedPoisonPill.Instance);
                        break;
                }
            }
        }

        [Fact]
        public async Task TaskRun_SyncHandler_ExecutedSequantially()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestRunActor(log));

            // Act
            actor.Tell("S:1");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), "X");

            // Assert
            Assert.Equal(new[] { "S:1" },
                         log);
        }

        [Fact]
        public async Task TaskRun_AtomicAsyncHandler_ExecutedSequantially()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestRunActor(log));

            // Act
            actor.Tell("A:1");
            actor.Tell("A:2");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), "X");

            // Assert
            Assert.Equal(new[] { "A:1", "A:1 done", "A:2", "A:2 done" },
                         log);
        }

        [Fact]
        public async Task TaskRun_AtomicAsyncHandler_ExecutedInterleaved()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestRunActor(log));

            // Act
            actor.Tell("R:1");
            actor.Tell("R:2");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), "X");

            // Assert
            Assert.Equal(new[] { "R:1", "R:2" },
                         log.Take(2));
            Assert.Equal(new[] { "R:1 done", "R:2 done" },
                         log.Skip(2).OrderBy(x => x));
        }
    }
}
