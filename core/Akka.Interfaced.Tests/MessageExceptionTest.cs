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
    public class MessageExceptionTest : TestKit.Xunit2.TestKit
    {
        public MessageExceptionTest(ITestOutputHelper output)
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"", output: output)
        {
        }

        public class TestExceptionActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestExceptionActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
                if (message == "E")
                    throw new Exception();
            }

            [MessageHandler]
            private async Task HandleAsync(int message)
            {
                _log.Add($"HandleAsync({message})");

                if (message == 1)
                    throw new Exception();

                await Task.Yield();

                _log.Add($"HandleAsync({message}) Done");

                if (message == 2)
                    throw new Exception();
            }

            [MessageHandler, Reentrant]
            private async Task HandleReentrantAsync(long message)
            {
                _log.Add($"HandleReentrantAsync({message})");

                if (message == 1)
                    throw new Exception();

                await Task.Yield();

                _log.Add($"HandleReentrantAsync({message}) Done");

                if (message == 2)
                    throw new Exception();
            }
        }
        [Fact]
        public void ExceptionThrown_At_Handle()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestExceptionActor(log)));

            // Act
            actor.Tell("E");

            // Assert
            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "Handle(E)" },
                         log);
        }

        [Fact]
        public void ExceptionThrown_At_HandleAsync()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestExceptionActor(log)));

            // Act
            actor.Tell(1);

            // Assert
            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleAsync(1)" },
                         log);
        }

        [Fact]
        public void ExceptionThrown_At_HandleAsyncDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestExceptionActor(log)));

            // Act
            actor.Tell(2);

            // Assert
            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleAsync(2)", "HandleAsync(2) Done" },
                         log);
        }

        [Fact]
        public void ExceptionThrown_At_HandleReentrantAsync()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestExceptionActor(log)));

            // Act
            actor.Tell(1L);

            // Assert
            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleReentrantAsync(1)" },
                         log);
        }

        [Fact]
        public void ExceptionThrown_At_HandleReentrantAsyncDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestExceptionActor(log)));

            // Act
            actor.Tell(2L);

            // Assert
            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleReentrantAsync(2)", "HandleReentrantAsync(2) Done" },
                         log);
        }
    }
}
