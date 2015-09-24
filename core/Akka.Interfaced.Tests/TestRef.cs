using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using System;
using System.Linq;

namespace Akka.Interfaced.Tests
{
    public class TestRefActor : InterfacedActor<TestRefActor>, IDummy
    {
        async Task<object> IDummy.Call(object param)
        {
            var list = (List<object>)param;
            var delay = (int)list[0];
            await Task.Delay(delay);
            return null;
        }
    }

    public class TestRef : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void Test_WithNoReply()
        {
            var actor = ActorOfAsTestActorRef<TestRefActor>();
            var a = new DummyRef(actor);
            a.WithNoReply().Call(100);
            ExpectNoMsg();
        }
    }
}
