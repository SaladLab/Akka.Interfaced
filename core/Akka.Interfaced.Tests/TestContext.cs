using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using System;
using System.Linq;

namespace Akka.Interfaced.Tests
{
    public class TestContextActor : InterfacedActor<TestContextActor>, IDummy
    {
        async Task<object> IDummy.Call(object param)
        {
            var list = (List<IUntypedActorContext>)(param);
            list.Add(Context);
            await Task.Yield();
            list.Add(Context);
            return null;
        }
    }

    public class TestContextReentrantActor : InterfacedActor<TestContextReentrantActor>, IDummy
    {
        [Reentrant]
        async Task<object> IDummy.Call(object param)
        {
            var list = (List<IUntypedActorContext>)(param);
            list.Add(Context);
            await Task.Yield();
            list.Add(Context);
            return null;
        }
    }

    public class TestContextMessageActor : InterfacedActor<TestContextMessageActor>
    {
        [MessageHandler]
        async Task OnMessage(List<IUntypedActorContext> list)
        {
            list.Add(Context);
            await Task.Yield();
            list.Add(Context);
            Sender.Tell(list.Count);
        }
    }

    public class TestContextMessageReentrantActor : InterfacedActor<TestContextMessageReentrantActor>
    {
        [MessageHandler, Reentrant]
        async Task OnMessage(List<IUntypedActorContext> list)
        {
            list.Add(Context);
            await Task.Yield();
            list.Add(Context);
            Sender.Tell(list.Count);
        }
    }

    public class TestContext : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public async Task Test_KeepContext_In_AsyncMethod()
        {
            var actor = ActorOfAsTestActorRef<TestContextActor>();
            var a = new DummyRef(actor);
            var contexts = new List<IUntypedActorContext>();
            await a.Call(contexts);
            Assert.Equal(contexts[0], contexts[1]);
        }

        [Fact]
        public async Task Test_KeepContext_In_AsyncReentrantMethod()
        {
            var actor = ActorOfAsTestActorRef<TestContextReentrantActor>();
            var a = new DummyRef(actor);
            var contexts = new List<IUntypedActorContext>();
            await a.Call(contexts);
            Assert.Equal(contexts[0], contexts[1]);
        }

        [Fact]
        public async Task Test_KeepContext_In_AsyncMessage()
        {
            var actor = ActorOfAsTestActorRef<TestContextMessageActor>();
            var list = new List<IUntypedActorContext>();
            var count = await actor.Ask<int>(list);
            Assert.Equal(list[0], list[1]);
        }

        [Fact]
        public async Task Test_KeepContext_In_AsyncReentrantMessage()
        {
            var actor = ActorOfAsTestActorRef<TestContextMessageReentrantActor>();
            var list = new List<IUntypedActorContext>();
            var count = await actor.Ask<int>(list);
            Assert.Equal(list[0], list[1]);
        }
    }
}
