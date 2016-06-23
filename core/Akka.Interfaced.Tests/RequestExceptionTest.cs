using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestExceptionTest : TestKit.Xunit2.TestKit
    {
        public RequestExceptionTest(ITestOutputHelper output)
            : base("akka.actor.guardian-supervisor-strategy=\"Akka.Actor.StoppingSupervisorStrategy\"", output: output)
        {
        }

        [ResponsiveException(typeof(ArgumentException))]
        public class TestExceptionActor : InterfacedActor, IExtendedInterface<IDummy>, IWorker
        {
            private LogBoard<string> _log;
            private int _delay;

            public TestExceptionActor(LogBoard<string> log, int delay)
            {
                _log = log;
                _delay = delay;
            }

            [ExtendedHandler]
            private object Call(object param)
            {
                _log.Add($"Call({param})");

                if ((string)param == "E")
                    throw new Exception();
                else if ((string)param == "e")
                    throw new ArgumentException();

                return param;
            }

            async Task IWorker.Atomic(int id)
            {
                _log.Add($"Atomic({id})");

                if (id == 1)
                    throw new Exception();
                else if (id == -1)
                    throw new ArgumentException();

                if (_delay == 0)
                    await Task.Yield();
                else
                    await Task.Delay(_delay);

                _log.Add($"Atomic({id}) Done");

                if (id == 2)
                    throw new Exception();
                else if (id == -2)
                    throw new ArgumentException();
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _log.Add($"Reentrant({id})");

                if (id == 1)
                    throw new Exception();
                else if (id == -1)
                    throw new ArgumentException();

                if (_delay == 0)
                    await Task.Yield();
                else
                    await Task.Delay(_delay);

                _log.Add($"Reentrant({id}) Done");

                if (id == 2)
                    throw new Exception();
                else if (id == -2)
                    throw new ArgumentException();
            }
        }

        [Fact]
        public async Task ExceptionThrown_At_Request()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 0))).Cast<DummyRef>();

            // Act
            var exception = await Record.ExceptionAsync(() => a.Call("E"));

            // Assert
            Assert.IsType<RequestFaultException>(exception);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Call(E)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_Request_With_ResponsiveException()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 0))).Cast<DummyRef>();

            // Act
            var exception = await Record.ExceptionAsync(() => a.Call("e"));

            // Assert
            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(new[] { "Call(e)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestAsync()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 0))).Cast<WorkerRef>();

            // Act
            var exception = await Record.ExceptionAsync(() => a.Atomic(1));

            // Assert
            Assert.IsType<RequestFaultException>(exception);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Atomic(1)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestAsyncDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 0))).Cast<WorkerRef>();

            // Act
            var exception = await Record.ExceptionAsync(() => a.Atomic(2));

            // Assert
            Assert.IsType<RequestFaultException>(exception);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Atomic(2)", "Atomic(2) Done" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestReentrantAsync()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 0))).Cast<WorkerRef>();

            // Act
            var exception = await Record.ExceptionAsync(() => a.Reentrant(1));

            // Assert
            Assert.IsType<RequestFaultException>(exception);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Reentrant(1)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestReentrantAsyncDone()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 0))).Cast<WorkerRef>();

            // Act
            var exception = await Record.ExceptionAsync(() => a.Reentrant(2));

            // Assert
            Assert.IsType<RequestFaultException>(exception);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Reentrant(2)", "Reentrant(2) Done" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_OngoingAtomicAsync_ThrownByActorStop()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 1000))).Cast<WorkerRef>();

            // Act
            var exceptionTask = Record.ExceptionAsync(() => a.Atomic(10));
            a.CastToIActorRef().Tell(PoisonPill.Instance); // dangerous but for test

            // Assert
            Assert.IsType<RequestHaltException>(await exceptionTask);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Atomic(10)" },
                         log);
        }

        [Fact]
        public async Task ExceptionThrown_At_RequestReentrantAsync_ThrownByActorStop()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(() => new TestExceptionActor(log, 1000))).Cast<WorkerRef>();

            // Act
            var exceptionTask1 = Record.ExceptionAsync(() => a.Reentrant(10));
            var exceptionTask2 = Record.ExceptionAsync(() => a.Reentrant(11));
            var exception = await Record.ExceptionAsync(() => a.Atomic(1));

            // Assert
            Assert.IsType<RequestFaultException>(exception);
            Assert.IsType<RequestHaltException>(await exceptionTask1);
            Assert.IsType<RequestHaltException>(await exceptionTask2);
            Watch(a.CastToIActorRef());
            ExpectTerminated(a.CastToIActorRef());
            Assert.Equal(new[] { "Reentrant(10)", "Reentrant(11)", "Atomic(1)" },
                         log);
        }
    }
}
