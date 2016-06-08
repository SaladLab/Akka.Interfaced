using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class NotificationExceptionTest : TestKit.Xunit2.TestKit
    {
        public NotificationExceptionTest(ITestOutputHelper output)
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"", output: output)
        {
        }

        public class TestExceptionActor : InterfacedActor, ISubjectObserver, ISubject2ObserverAsync
        {
            private LogBoard<string> _log;

            public TestExceptionActor(LogBoard<string> log)
            {
                _log = log;
            }

            void ISubjectObserver.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
                if (eventName == "E")
                    throw new Exception();
            }

            async Task ISubject2ObserverAsync.Event(string eventName)
            {
                _log.Add($"Event({eventName})");

                if (eventName == "E")
                    throw new Exception();

                await Task.Yield();

                _log.Add($"Event({eventName}) Done");

                if (eventName == "F")
                    throw new Exception();
            }

            [Reentrant]
            async Task ISubject2ObserverAsync.Event2(string eventName)
            {
                _log.Add($"Event2({eventName})");

                if (eventName == "E")
                    throw new Exception();

                await Task.Yield();

                _log.Add($"Event2({eventName}) Done");

                if (eventName == "F")
                    throw new Exception();
            }
        }

        private async Task<Tuple<SubjectRef, IActorRef>> SetupActors(LogBoard<string> log)
        {
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = new SubjectRef(subjectActor);
            var observingActor = ActorOf(() => new TestExceptionActor(log));
            await subject.Subscribe(new SubjectObserver(observingActor));
            return Tuple.Create(subject, (IActorRef)observingActor);
        }

        private async Task<Tuple<Subject2Ref, IActorRef>> SetupActors2(LogBoard<string> log)
        {
            var subjectActor = ActorOfAsTestActorRef<Subject2Actor>("Subject");
            var subject = new Subject2Ref(subjectActor);
            var observingActor = ActorOf(() => new TestExceptionActor(log));
            await subject.Subscribe(new Subject2Observer(observingActor));
            return Tuple.Create(subject, (IActorRef)observingActor);
        }

        [Fact]
        public async Task ExceptionThrown_At_Notification()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors(log);

            // Act
            await actors.Item1.MakeEvent("E");

            // Assert
            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event(E)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationAsync()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors2(log);

            // Act
            await actors.Item1.MakeEvent("E");

            // Assert
            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event(E)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationAsyncDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors2(log);

            // Act
            await actors.Item1.MakeEvent("F");

            // Assert
            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event(F)", "Event(F) Done" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationReentrantAsync()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors2(log);

            // Act
            await actors.Item1.MakeEvent2("E");

            // Assert
            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event2(E)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationReentrantAsyncDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors2(log);

            // Act
            await actors.Item1.MakeEvent2("F");

            // Assert
            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event2(F)", "Event2(F) Done" },
                         log);
        }
    }
}
