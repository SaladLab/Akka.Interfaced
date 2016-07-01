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
    public class MessageGenericTest : TestKit.Xunit2.TestKit
    {
        public MessageGenericTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public static class Messages
        {
            public class Message
            {
                public string Value;
            }

            public class Message<T>
            {
                public T Value;
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
            private void OnMessage(Messages.Message m)
            {
                _log.Add("OnMessage:" + m.Value);
            }

            [MessageHandler]
            private void OnMessage<T>(Messages.Message<T> m)
            {
                _log.Add("OnMessage<T>:" + m.Value);
            }
        }

        public class TestMessageActor<T> : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestMessageActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void OnMessage(Messages.Message m)
            {
                _log.Add("OnMessage:" + m.Value);
            }

            [MessageHandler]
            private void OnMessage(Messages.Message<T> m)
            {
                _log.Add("OnMessage<T>:" + m.Value);
            }
        }

        [Theory]
        [InlineData(typeof(TestMessageActor))]
        [InlineData(typeof(TestMessageActor<string>))]
        public async Task HandleMessage(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(actorType, log));

            // Act
            actor.Tell(new Messages.Message { Value = "A" });
            actor.Tell(new Messages.Message<string> { Value = "B" });
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(new[] { "OnMessage:A", "OnMessage<T>:B" },
                         log);
        }
    }
}
