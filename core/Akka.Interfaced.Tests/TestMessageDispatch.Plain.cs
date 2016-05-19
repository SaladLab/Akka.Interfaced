using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    public static class PlainMessages
    {
        public class Func
        {
            public string Value;
        }

        public class TaskAtomic
        {
            public string Value;
        }

        public class TaskReentrant
        {
            public string Value;
        }
    }

    public class TestMessageDispatchPlainActor : InterfacedActor, IDummy
    {
        private List<string> _eventLog;

        public TestMessageDispatchPlainActor(List<string> eventLog)
        {
            _eventLog = eventLog;
        }

        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }

        [MessageHandler]
        private void OnMessage(PlainMessages.Func message)
        {
            _eventLog.Add(message.Value + "_1");
        }

        [MessageHandler]
        private async Task OnMessage(PlainMessages.TaskAtomic message)
        {
            _eventLog.Add(message.Value + "_1");
            await Task.Delay(10);
            _eventLog.Add(message.Value + "_2");
        }

        [MessageHandler, Reentrant]
        private async Task OnMessage(PlainMessages.TaskReentrant message)
        {
            _eventLog.Add(message.Value + "_1");
            await Task.Delay(10);
            _eventLog.Add(message.Value + "_2");
        }
    }

    public class TestMessageDispatchPlain : Akka.TestKit.Xunit2.TestKit
    {
        public TestMessageDispatchPlain(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_can_handle_plain_message()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestMessageDispatchPlainActor>(
                Props.Create<TestMessageDispatchPlainActor>(eventLog));

            actor.Tell(new PlainMessages.Func { Value = "A" });

            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(new List<string> { "A_1" }, eventLog);
        }

        [Fact]
        public async Task Test_can_handle_plain_message_in_atomic_way()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestMessageDispatchPlainActor>(
                Props.Create<TestMessageDispatchPlainActor>(eventLog));

            actor.Tell(new PlainMessages.TaskAtomic { Value = "A" });
            actor.Tell(new PlainMessages.TaskAtomic { Value = "B" });

            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(new List<string> { "A_1", "A_2", "B_1", "B_2" }, eventLog);
        }

        [Fact]
        public async Task Test_can_handle_plain_message_in_reentrant_way()
        {
            var eventLog = new List<string>();
            var actor = ActorOfAsTestActorRef<TestMessageDispatchPlainActor>(
                Props.Create<TestMessageDispatchPlainActor>(eventLog));

            actor.Tell(new PlainMessages.TaskReentrant { Value = "A" });
            actor.Tell(new PlainMessages.TaskReentrant { Value = "B" });

            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal("A_1", eventLog[0]);
            Assert.Equal("B_1", eventLog[1]);
        }
    }
}
