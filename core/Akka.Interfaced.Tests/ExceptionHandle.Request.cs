using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;

namespace Akka.Interfaced.Tests
{
    [ResponsiveException(typeof(ArgumentException))]
    public class ExceptionActor_Request : InterfacedActor, IExtendedInterface<IDummy>, IWorker
    {
        private LogBoard _log;
        private int _delay;

        public ExceptionActor_Request(LogBoard log, int delay)
        {
            _log = log;
            _delay = delay;
        }

        [ExtendedHandler]
        private object Call(object param)
        {
            _log.Log($"Call({param})");

            if ((string)param == "E")
                throw new Exception();
            else if ((string)param == "e")
                throw new ArgumentException();

            return param;
        }

        async Task IWorker.Atomic(int id)
        {
            _log.Log($"Atomic({id})");

            if (id == 1)
                throw new Exception();
            else if (id == -1)
                throw new ArgumentException();

            if (_delay == 0)
                await Task.Yield();
            else
                await Task.Delay(_delay);

            _log.Log($"Atomic({id}) Done");

            if (id == 2)
                throw new Exception();
            else if (id == -2)
                throw new ArgumentException();
        }

        [Reentrant]
        async Task IWorker.Reentrant(int id)
        {
            _log.Log($"Reentrant({id})");

            if (id == 1)
                throw new Exception();
            else if (id == -1)
                throw new ArgumentException();

            if (_delay == 0)
                await Task.Yield();
            else
                await Task.Delay(_delay);

            _log.Log($"Reentrant({id}) Done");

            if (id == 2)
                throw new Exception();
            else if (id == -2)
                throw new ArgumentException();
        }
    }

    public class ExceptionHandle_Request : Akka.TestKit.Xunit2.TestKit
    {
        public ExceptionHandle_Request()
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"")
        {
        }

        [Fact]
        public async Task ExceptionThrown_At_Request()
        {
            var log = new LogBoard();
            var dummy = new DummyRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 0))));

            var exception = await Record.ExceptionAsync(() => dummy.Call("E"));
            Assert.IsType<RequestFaultException>(exception);

            Watch(dummy.Actor);
            ExpectTerminated(dummy.Actor);
            Assert.Equal(new[] { "Call(E)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_Request_With_ResponsiveException()
        {
            var log = new LogBoard();
            var dummy = new DummyRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 0))));

            var exception = await Record.ExceptionAsync(() => dummy.Call("e"));
            Assert.IsType<ArgumentException>(exception);

            Assert.Equal(new[] { "Call(e)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestAsync()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 0))));

            var exception = await Record.ExceptionAsync(() => worker.Atomic(1));
            Assert.IsType<RequestFaultException>(exception);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            Assert.Equal(new[] { "Atomic(1)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestAsyncDone()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 0))));

            var exception = await Record.ExceptionAsync(() => worker.Atomic(2));
            Assert.IsType<RequestFaultException>(exception);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            Assert.Equal(new[] { "Atomic(2)", "Atomic(2) Done" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestReentrantAsync()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 0))));

            var exception = await Record.ExceptionAsync(() => worker.Reentrant(1));
            Assert.IsType<RequestFaultException>(exception);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            Assert.Equal(new[] { "Reentrant(1)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestReentrantAsyncDone()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 0))));

            var exception = await Record.ExceptionAsync(() => worker.Reentrant(2));
            Assert.IsType<RequestFaultException>(exception);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            Assert.Equal(new[] { "Reentrant(2)", "Reentrant(2) Done" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_OngoingAtomicAsync_ThrownByActorStop()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 1000))));

            var exceptionTask = Record.ExceptionAsync(() => worker.Atomic(10));
            worker.Actor.Tell(PoisonPill.Instance); // dangerous but for test
            Assert.IsType<RequestHaltException>(await exceptionTask);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            Assert.Equal(new[] { "Atomic(10)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestReentrantAsync_ThrownByActorStop()
        {
            var log = new LogBoard();
            var worker = new WorkerRef(ActorOf(Props.Create(() => new ExceptionActor_Request(log, 1000))));

            var exceptionTask1 = Record.ExceptionAsync(() => worker.Reentrant(10));
            var exceptionTask2 = Record.ExceptionAsync(() => worker.Reentrant(11));
            var exception = await Record.ExceptionAsync(() => worker.Atomic(1));
            Assert.IsType<RequestFaultException>(exception);
            Assert.IsType<RequestHaltException>(await exceptionTask1);
            Assert.IsType<RequestHaltException>(await exceptionTask2);

            Watch(worker.Actor);
            ExpectTerminated(worker.Actor);
            Assert.Equal(new[] { "Reentrant(10)", "Reentrant(11)", "Atomic(1)" },
                         log.GetAndClearLogs());
        }
    }
}
