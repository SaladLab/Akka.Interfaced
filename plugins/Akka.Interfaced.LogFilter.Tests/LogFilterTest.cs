using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Akka.Actor;

namespace Akka.Interfaced.LogFilter.Tests
{
    public class LogFilterTest : Akka.TestKit.Xunit2.TestKit
    {
        private NLog.Targets.MemoryTarget GetLogger()
        {
            return (NLog.Targets.MemoryTarget)NLog.LogManager.Configuration.FindTargetByName("Memory");
        }

        private void ClearLogger()
        {
            GetLogger().Logs.Clear();
        }

        private IList<string> GetLogs()
        {
            return GetLogger().Logs;
        }

        [Fact]
        public async Task Test_Paramter()
        {
            ClearLogger();

            var actor = ActorOfAsTestActorRef<TestActor>(Props.Create<TestActor>());
            var a = new TestRef(actor);
            await a.Call("Test");

            var logs = GetLogs();
            Assert.Equal(3, logs.Count);
            Assert.Equal("#-1 -> Call {\"value\":\"Test\"}", logs[0]);
            Assert.Equal("Call(Test)", logs[1]);
            Assert.Equal("#-1 <- Call <void>", logs[2]);
        }

        [Fact]
        public async Task Test_Surrogate_Paramter()
        {
            ClearLogger();

            var actor = ActorOfAsTestActorRef<TestActor>(Props.Create<TestActor>(), "TA");
            var a = new TestRef(actor);
            await a.CallWithActor(a);

            var logs = GetLogs();
            Assert.Equal(3, logs.Count);
            // ignore logs[0] because too verbose
            Assert.Equal("CallWithActor(akka://test/user/TA)", logs[1]);
            Assert.Equal("#-1 <- CallWithActor <void>", logs[2]);
        }

        [Fact]
        public async Task Test_Result()
        {
            ClearLogger();

            var actor = ActorOfAsTestActorRef<TestActor>(Props.Create<TestActor>());
            var a = new TestRef(actor);
            var ret = await a.GetHelloCount();
            Assert.Equal(0, ret);

            var logs = GetLogs();
            Assert.Equal(3, logs.Count);
            Assert.Equal("#-1 -> GetHelloCount {}", logs[0]);
            Assert.Equal("GetHelloCount()", logs[1]);
            Assert.Equal("#-1 <- GetHelloCount 0", logs[2]);
        }

        [Fact]
        public async Task Test_Exception()
        {
            ClearLogger();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var actor = ActorOfAsTestActorRef<TestActor>(Props.Create<TestActor>());
                var a = new TestRef(actor);
                await a.Call(null);
            });

            var logs = GetLogs();
            Assert.Equal(2, logs.Count);
            Assert.Equal("#-1 -> Call {}", logs[0]);
            Assert.True(logs[1].StartsWith("#-1 <- Call Exception: System.ArgumentNullException:"));
        }

        [Fact]
        public async Task Test_Paramter_And_Result()
        {
            ClearLogger();

            var actor = ActorOfAsTestActorRef<TestActor>(Props.Create<TestActor>());
            var a = new TestRef(actor);
            var ret = await a.SayHello("World");
            Assert.Equal("Hello World", ret);

            var logs = GetLogs();
            Assert.Equal(3, logs.Count);
            Assert.Equal("#-1 -> SayHello {\"name\":\"World\"}", logs[0]);
            Assert.Equal("SayHello(World)", logs[1]);
            Assert.Equal("#-1 <- SayHello \"Hello World\"", logs[2]);
        }
    }
}
