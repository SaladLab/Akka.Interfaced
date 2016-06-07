using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestDispatchTest : TestKit.Xunit2.TestKit
    {
        public RequestDispatchTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestScheduleActor : InterfacedActor, IWorker
        {
            private LogBoard<Tuple<int, int>> _logBoard;

            public TestScheduleActor(LogBoard<Tuple<int, int>> logBoard)
            {
                _logBoard = logBoard;
            }

            async Task IWorker.Atomic(int id)
            {
                _logBoard.Log(Tuple.Create(id, 1));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, 2));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, 3));
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _logBoard.Log(Tuple.Create(id, 1));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, 2));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, 3));
            }
        }

        [Fact]
        public async Task Schedule_AtomicHandler_Sequential()
        {
            // Arrange
            var logBoard = new LogBoard<Tuple<int, int>>();
            var a = new WorkerRef(ActorOf(() => new TestScheduleActor(logBoard)));

            // Act
            var t1 = a.Atomic(1);
            var t2 = a.Atomic(2);
            await Task.WhenAll(t1, t2);

            // Assert
            Assert.Equal(new Tuple<int, int>[]
            {
                Tuple.Create(1, 1),
                Tuple.Create(1, 2),
                Tuple.Create(1, 3),
                Tuple.Create(2, 1),
                Tuple.Create(2, 2),
                Tuple.Create(2, 3),
            }, logBoard.GetLogs());
        }

        [Fact]
        public async Task Schedule_ReentrantHandler_Interleaved()
        {
            // Arrange
            var logBoard = new LogBoard<Tuple<int, int>>();
            var a = new WorkerRef(ActorOf(() => new TestScheduleActor(logBoard)));

            // Act
            var t1 = a.Reentrant(1);
            var t2 = a.Reentrant(2);
            await Task.WhenAll(t1, t2);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 },
                         logBoard.GetLogs().Where(t => t.Item1 == 1).Select(t => t.Item2));
            Assert.Equal(new[] { 1, 2, 3 },
                         logBoard.GetLogs().Where(t => t.Item1 == 1).Select(t => t.Item2));
        }

        [Fact]
        public async Task Schedule_AtomicAndReentrantHandler_Interleaved()
        {
            // Arrange
            var logBoard = new LogBoard<Tuple<int, int>>();
            var a = new WorkerRef(ActorOf(() => new TestScheduleActor(logBoard)));

            // Act
            var t1 = a.Reentrant(1);
            var t2 = a.Atomic(2);
            await Task.WhenAll(t1, t2);

            // Assert
            Assert.Equal(new Tuple<int, int>[]
            {
                Tuple.Create(1, 1),
                Tuple.Create(2, 1),
                Tuple.Create(2, 2),
                Tuple.Create(2, 3),
                Tuple.Create(1, 2),
                Tuple.Create(1, 3),
            }, logBoard.GetLogs());
        }

        public class TestContextActor : InterfacedActor, IWorker
        {
            private LogBoard<Tuple<int, object>> _logBoard;

            public TestContextActor(LogBoard<Tuple<int, object>> logBoard)
            {
                _logBoard = logBoard;
            }

            // InterfacedActor tries to keep sender correctly!
            private object CurrentContext => Sender;

            async Task IWorker.Atomic(int id)
            {
                _logBoard.Log(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, CurrentContext));
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _logBoard.Log(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _logBoard.Log(Tuple.Create(id, CurrentContext));
            }
        }

        [Fact]
        public async Task Dispatch_AtomicHandlers_KeepContext()
        {
            // Arrange
            var logBoard = new LogBoard<Tuple<int, object>>();
            var a = new WorkerRef(ActorOf(() => new TestContextActor(logBoard)));

            // Act
            var t1 = a.Atomic(1);
            var t2 = a.Atomic(2);
            await Task.WhenAll(t1, t2);

            // Assetr
            var logs = logBoard.GetLogs();
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(2, logs.Select(t => t.Item2).Distinct().Count());
        }

        [Fact]
        public async Task Dispatch_ReentrantHandlers_KeepContext()
        {
            // Arrange
            var logBoard = new LogBoard<Tuple<int, object>>();
            var a = new WorkerRef(ActorOf(() => new TestContextActor(logBoard)));

            // Act
            var t1 = a.Reentrant(1);
            var t2 = a.Reentrant(2);
            await Task.WhenAll(t1, t2);

            // Assetr
            var logs = logBoard.GetLogs();
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(2, logs.Select(t => t.Item2).Distinct().Count());
        }
    }
}
