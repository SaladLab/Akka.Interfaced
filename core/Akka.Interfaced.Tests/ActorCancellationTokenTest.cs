using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class ActorCancellationTokenTest : TestKit.Xunit2.TestKit
    {
        public ActorCancellationTokenTest(ITestOutputHelper output)
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"", output: output)
        {
        }

        public class TaskCancellationActor : InterfacedActor, IWorker, ISubjectObserverAsync
        {
            private LogBoard<string> _log;
            private int _delay;

            public TaskCancellationActor(LogBoard<string> log, int delay)
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
                _log.Add($"Handle({id})");

                if (_delay == 0)
                    await Task.Yield();
                else
                    await Task.Delay(_delay, CancellationToken);

                _log.Add($"Handle({id}) Done");
            }

            Task IWorker.Atomic(int id)
            {
                return Task.FromResult(id);
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _log.Add($"Reentrant({id})");

                if (_delay == 0)
                    await Task.Yield();
                else
                    await Task.Delay(_delay, CancellationToken);

                _log.Add($"Reentrant({id}) Done");
            }

            [Reentrant]
            async Task ISubjectObserverAsync.Event(string eventName)
            {
                _log.Add($"Event({eventName})");

                if (_delay == 0)
                    await Task.Yield();
                else
                    await Task.Delay(_delay, CancellationToken);

                _log.Add($"Event({eventName}) Done");
            }
        }

        [Fact]
        public async Task StopActor_RunningAsyncRequestHandler_Canceled()
        {
            var log = new LogBoard<string>();
            var worker = new WorkerRef(ActorOf(() => new TaskCancellationActor(log, 100)));

            var exceptionTask = Record.ExceptionAsync(() => worker.Reentrant(1));
            worker.Actor.Tell("E");
            Assert.IsType<RequestHaltException>(await exceptionTask);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            await Task.Delay(100);

            Assert.Equal(new[] { "Reentrant(1)" }, log);
        }

        [Fact]
        public async Task StopActor_RunningAsyncNotificationHandler_Canceled()
        {
            var log = new LogBoard<string>();

            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = new SubjectRef(subjectActor);
            var observingActor = ActorOf(() => new TaskCancellationActor(log, 100));
            await subject.Subscribe(new SubjectObserver(observingActor));

            await subject.MakeEvent("E");
            observingActor.Tell("E");

            Watch(observingActor);
            ExpectTerminated(observingActor);
            await Task.Delay(100);
            Assert.Equal(new[] { "Event(E)" }, log);
        }

        [Fact]
        public async Task StopActor_RunningAsyncMessageHandler_Canceled()
        {
            var log = new LogBoard<string>();
            var worker = new WorkerRef(ActorOf(() => new TaskCancellationActor(log, 100)));

            worker.Actor.Tell(1);
            worker.Actor.Tell("E");

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            await Task.Delay(100);
            Assert.Equal(new[] { "Handle(1)" }, log);
        }
    }
}
