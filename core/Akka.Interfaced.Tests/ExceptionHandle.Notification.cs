using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;

namespace Akka.Interfaced.Tests
{
    public class ExceptionActor_Notification : InterfacedActor, ISubjectObserver, IExtendedInterface<ISubject2Observer>
    {
        private LogBoard _log;

        public ExceptionActor_Notification(LogBoard log)
        {
            _log = log;
        }

        void ISubjectObserver.Event(string eventName)
        {
            _log.Log($"Event({eventName})");
            if (eventName == "E")
                throw new Exception();
        }

        [ExtendedHandler]
        private async Task Event(string eventName)
        {
            _log.Log($"Event({eventName})");

            if (eventName == "E")
                throw new Exception();

            await Task.Yield();

            _log.Log($"Event({eventName}) Done");

            if (eventName == "F")
                throw new Exception();
        }

        [ExtendedHandler, Reentrant]
        private async Task Event2(string eventName)
        {
            _log.Log($"Event2({eventName})");

            if (eventName == "E")
                throw new Exception();

            await Task.Yield();

            _log.Log($"Event2({eventName}) Done");

            if (eventName == "F")
                throw new Exception();
        }
    }

    public class ExceptionHandle_Notification : Akka.TestKit.Xunit2.TestKit
    {
        public ExceptionHandle_Notification()
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"")
        {
        }

        private async Task<Tuple<SubjectRef, IActorRef>> SetupActors(LogBoard log)
        {
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = new SubjectRef(subjectActor);
            var observingActor = ActorOf(() => new ExceptionActor_Notification(log));
            await subject.Subscribe(new SubjectObserver(observingActor));
            return Tuple.Create(subject, (IActorRef)observingActor);
        }

        private async Task<Tuple<Subject2Ref, IActorRef>> SetupActors2(LogBoard log)
        {
            var subjectActor = ActorOfAsTestActorRef<Subject2Actor>("Subject");
            var subject = new Subject2Ref(subjectActor);
            var observingActor = ActorOf(() => new ExceptionActor_Notification(log));
            await subject.Subscribe(new Subject2Observer(observingActor));
            return Tuple.Create(subject, (IActorRef)observingActor);
        }

        [Fact]
        public async Task ExceptionThrown_At_Notification()
        {
            var log = new LogBoard();
            var actors = await SetupActors(log);

            await actors.Item1.MakeEvent("E");

            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event(E)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationAsync()
        {
            var log = new LogBoard();
            var actors = await SetupActors2(log);

            await actors.Item1.MakeEvent("E");

            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event(E)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationAsyncDone()
        {
            var log = new LogBoard();
            var actors = await SetupActors2(log);

            await actors.Item1.MakeEvent("F");

            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event(F)", "Event(F) Done" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationReentrantAsync()
        {
            var log = new LogBoard();
            var actors = await SetupActors2(log);

            await actors.Item1.MakeEvent2("E");

            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event2(E)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_NotificationReentrantAsyncDone()
        {
            var log = new LogBoard();
            var actors = await SetupActors2(log);

            await actors.Item1.MakeEvent2("F");

            Watch(actors.Item2);
            ExpectTerminated(actors.Item2);
            Assert.Equal(new[] { "Event2(F)", "Event2(F) Done" },
                         log.GetAndClearLogs());
        }
    }
}
