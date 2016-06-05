using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    public class DummyFinalActor : InterfacedActor, IDummyExFinal
    {
        public DummyFinalActor()
        {
        }

        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult<object>("Call:" + param);
        }

        Task<object> IDummyEx.CallEx(object param)
        {
            return Task.FromResult<object>("CallEx:" + param);
        }

        Task<object> IDummyEx2.CallEx2(object param)
        {
            return Task.FromResult<object>("CallEx2:" + param);
        }

        Task<object> IDummyExFinal.CallExFinal(object param)
        {
            return Task.FromResult<object>("CallExFinal:" + param);
        }
    }

    public class InheritanceTest : Akka.TestKit.Xunit2.TestKit
    {
        public InheritanceTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_InheritedInterfacedActor_Work()
        {
            var actor = ActorOfAsTestActorRef<DummyFinalActor>();
            var a = new DummyExFinalRef(actor);
            Assert.Equal("Call:1", await a.Call("1"));
            Assert.Equal("CallEx:1", await a.CallEx("1"));
            Assert.Equal("CallEx2:1", await a.CallEx2("1"));
            Assert.Equal("CallExFinal:1", await a.CallExFinal("1"));
        }
    }
}
