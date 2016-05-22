using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;

namespace Akka.Interfaced.Tests
{
    public class TaskCancellationActor : InterfacedActor, IWorker, IExtendedInterface<ISubjectObserver>
    {
        private LogBoard _log;
        private int _delay;

        public TaskCancellationActor(LogBoard log, int delay)
        {
            _log = log;
            _delay = delay;
        }

        [MessageHandler]
        private void Handle(string id)
        {
            throw new Exception("");
        }

        [MessageHandler, Reentrant]
        private async Task Handle(int id)
        {
            _log.Log($"Handle({id})");

            if (_delay == 0)
                await Task.Yield();
            else
                await Task.Delay(_delay, CancellationToken);

            _log.Log($"Handle({id}) Done");
        }

        Task IWorker.Atomic(int id)
        {
            return Task.FromResult(id);
        }

        [Reentrant]
        async Task IWorker.Reentrant(int id)
        {
            _log.Log($"Reentrant({id})");

            if (_delay == 0)
                await Task.Yield();
            else
                await Task.Delay(_delay, CancellationToken);

            _log.Log($"Reentrant({id}) Done");
        }

        [ExtendedHandler, Reentrant]
        private async Task Event(string eventName)
        {
            _log.Log($"Event({eventName})");

            if (_delay == 0)
                await Task.Yield();
            else
                await Task.Delay(_delay, CancellationToken);

            _log.Log($"Event({eventName}) Done");
        }
    }

    public class ActorTaskCancellation : Akka.TestKit.Xunit2.TestKit
    {
        public ActorTaskCancellation()
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"")
        {
        }

        [Fact]
        public async Task TaskInRequest_WhenActorStop_Cancelled()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(() => new TaskCancellationActor(log, 100)));

            var exceptionTask = Record.ExceptionAsync(() => worker.Reentrant(1));
            worker.Actor.Tell("E");
            Assert.IsType<RequestHaltException>(await exceptionTask);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            await Task.Delay(100);

            Assert.Equal(new[] { "Reentrant(1)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task TaskInNotification_WhenActorStop_Cancelled()
        {
            var log = new LogBoard();

            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = new SubjectRef(subjectActor);
            var observingActor = ActorOf(() => new TaskCancellationActor(log, 100));
            await subject.Subscribe(new SubjectObserver(observingActor));

            await subject.MakeEvent("E");
            observingActor.Tell("E");

            Watch(observingActor);
            ExpectTerminated(observingActor);
            await Task.Delay(100);
            Assert.Equal(new[] { "Event(E)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task TaskInMessage_WhenActorStop_Cancelled()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(() => new TaskCancellationActor(log, 100)));

            worker.Actor.Tell(1);
            worker.Actor.Tell("E");

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            await Task.Delay(100);
            Assert.Equal(new[] { "Handle(1)" },
                         log.GetAndClearLogs());
        }
    }
}
