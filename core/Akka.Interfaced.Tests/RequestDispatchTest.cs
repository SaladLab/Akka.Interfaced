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
            private LogBoard<Tuple<int, int>> _log;

            public TestScheduleActor(LogBoard<Tuple<int, int>> log)
            {
                _log = log;
            }

            async Task IWorker.Atomic(int id)
            {
                _log.Add(Tuple.Create(id, 1));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, 2));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, 3));
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _log.Add(Tuple.Create(id, 1));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, 2));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, 3));
            }
        }

        [Fact]
        public async Task Schedule_AtomicHandler_Sequential()
        {
            // Arrange
            var log = new LogBoard<Tuple<int, int>>();
            var a = ActorOf(() => new TestScheduleActor(log)).Cast<WorkerRef>();

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
            }, log);
        }

        [Fact]
        public async Task Schedule_ReentrantHandler_Interleaved()
        {
            // Arrange
            var log = new LogBoard<Tuple<int, int>>();
            var a = ActorOf(() => new TestScheduleActor(log)).Cast<WorkerRef>();

            // Act
            var t1 = a.Reentrant(1);
            var t2 = a.Reentrant(2);
            await Task.WhenAll(t1, t2);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 },
                         log.Where(t => t.Item1 == 1).Select(t => t.Item2));
            Assert.Equal(new[] { 1, 2, 3 },
                         log.Where(t => t.Item1 == 1).Select(t => t.Item2));
        }

        [Fact]
        public async Task Schedule_AtomicAndReentrantHandler_Interleaved()
        {
            // Arrange
            var log = new LogBoard<Tuple<int, int>>();
            var a = ActorOf(() => new TestScheduleActor(log)).Cast<WorkerRef>();

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
            }, log);
        }

        public class TestContextActor : InterfacedActor, IWorker
        {
            private LogBoard<Tuple<int, object>> _log;

            public TestContextActor(LogBoard<Tuple<int, object>> log)
            {
                _log = log;
            }

            // InterfacedActor tries to keep sender correctly!
            private object CurrentContext => Sender;

            async Task IWorker.Atomic(int id)
            {
                _log.Add(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, CurrentContext));
            }

            [Reentrant]
            async Task IWorker.Reentrant(int id)
            {
                _log.Add(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, CurrentContext));
                await Task.Delay(10);
                _log.Add(Tuple.Create(id, CurrentContext));
            }
        }

        [Fact]
        public async Task Dispatch_AtomicHandlers_KeepContext()
        {
            // Arrange
            var log = new LogBoard<Tuple<int, object>>();
            var a = ActorOf(() => new TestContextActor(log)).Cast<WorkerRef>();

            // Act
            var t1 = a.Atomic(1);
            var t2 = a.Atomic(2);
            await Task.WhenAll(t1, t2);

            // Assetr
            var logs = log;
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(2, logs.Select(t => t.Item2).Distinct().Count());
        }

        [Fact]
        public async Task Dispatch_ReentrantHandlers_KeepContext()
        {
            // Arrange
            var log = new LogBoard<Tuple<int, object>>();
            var a = ActorOf(() => new TestContextActor(log)).Cast<WorkerRef>();

            // Act
            var t1 = a.Reentrant(1);
            var t2 = a.Reentrant(2);
            await Task.WhenAll(t1, t2);

            // Assetr
            var logs = log;
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(1, logs.Where(t => t.Item1 == 1).Select(t => t.Item2).Distinct().Count());
            Assert.Equal(2, logs.Select(t => t.Item2).Distinct().Count());
        }
    }
}
