using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class MessageDispatchTest : TestKit.Xunit2.TestKit
    {
        public MessageDispatchTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public static class PlainMessages
        {
            public class Func
            {
                public string Value;
            }

            public class TaskAtomic
            {
                public string Value;
            }

            public class TaskReentrant
            {
                public string Value;
            }
        }

        public class TestMessageActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestMessageActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void OnMessage(PlainMessages.Func message)
            {
                _log.Add(message.Value + "_1");
            }

            [MessageHandler]
            private async Task OnMessage(PlainMessages.TaskAtomic message)
            {
                _log.Add(message.Value + "_1");
                await Task.Delay(10);
                _log.Add(message.Value + "_2");
            }

            [MessageHandler, Reentrant]
            private async Task OnMessage(PlainMessages.TaskReentrant message)
            {
                _log.Add(message.Value + "_1");
                await Task.Delay(10);
                _log.Add(message.Value + "_2");
            }
        }

        [Fact]
        public async Task Message_SyncHandler_ExecutedSequantially()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestMessageActor(log));

            // Act
            actor.Tell(new PlainMessages.Func { Value = "A" });
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(new[] { "A_1" },
                         log);
        }

        [Fact]
        public async Task Message_AtomicAsyncHandler_ExecutedSequantially()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestMessageActor(log));

            // Act
            actor.Tell(new PlainMessages.TaskAtomic { Value = "A" });
            actor.Tell(new PlainMessages.TaskAtomic { Value = "B" });
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(new[] { "A_1", "A_2", "B_1", "B_2" },
                         log);
        }

        [Fact]
        public async Task Message_AtomicAsyncHandler_ExecutedInterleaved()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestMessageActor(log));

            // Act
            actor.Tell(new PlainMessages.TaskReentrant { Value = "A" });
            actor.Tell(new PlainMessages.TaskReentrant { Value = "B" });
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(new[] { "A_1", "B_1" },
                         log.Take(2));
            Assert.Equal(new[] { "A_2", "B_2" },
                         log.Skip(2).OrderBy(x => x));
        }
    }
}
