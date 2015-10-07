using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using System;
using System.Linq;

namespace Akka.Interfaced.Tests
{
    public class TestPoisonPillActor : InterfacedActor<TestPoisonPillActor>, IWorker
    {
        private readonly List<string> _eventLog;

        public TestPoisonPillActor(List<string> eventLog)
        {
            _eventLog = eventLog;
        }

        protected override async Task OnPreStop()
        {
            _eventLog.Add("OnPreStop");
            await Task.Delay(10);
            _eventLog.Add("OnPreStop done");
        }

        async Task IWorker.Atomic(int id)
        {
            _eventLog.Add($"Atomic({id})");
            await Task.Delay(10);
            _eventLog.Add($"Atomic({id}) done");
        }

        [Reentrant]
        async Task IWorker.Reentrant(int id)
        {
            _eventLog.Add($"Reentrant({id})");
            await Task.Delay(10);
            _eventLog.Add($"Reentrant({id}) done");
        }
    }

    public class TestPoisonPill : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public async Task Test_Actor_WaitFor_All_AtomicHandler_When_PoisonPill()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestPoisonPillActor>(Props.Create<TestPoisonPillActor>(eventLog));
            var w = new WorkerRef(actor);
            w.WithNoReply().Atomic(1);
            w.WithNoReply().Atomic(2);
            await w.Actor.GracefulStop(TimeSpan.FromMinutes(5), InterfacedPoisonPill.Instance);
            Assert.Equal(new List<string>
            {
                "Atomic(1)",
                "Atomic(1) done",
                "Atomic(2)",
                "Atomic(2) done",
                "OnPreStop",
                "OnPreStop done"
            }, eventLog);
        }

        [Fact]
        public async Task Test_Actor_WaitFor_All_ReentrantHandler_When_PoisonPill()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestPoisonPillActor>(Props.Create<TestPoisonPillActor>(eventLog));
            var w = new WorkerRef(actor);
            w.WithNoReply().Reentrant(1);
            w.WithNoReply().Reentrant(2);
            await w.Actor.GracefulStop(TimeSpan.FromMinutes(5), InterfacedPoisonPill.Instance);
            Assert.True(new HashSet<string>
            {
                "Reentrant(1)",
                "Reentrant(1) done",
                "Reentrant(2)",
                "Reentrant(2) done",
            }.SetEquals(new HashSet<string>(eventLog.Take(4))));
            Assert.Equal(new List<string>
            {
                "OnPreStop",
                "OnPreStop done"
            }, eventLog.Skip(4));
        }
    }
}
