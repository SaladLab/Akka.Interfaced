using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestInheritanceTest : TestKit.Xunit2.TestKit
    {
        public RequestInheritanceTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

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

        public class DummyFinalSyncActor : InterfacedActor, IDummyExFinalSync
        {
            public DummyFinalSyncActor()
            {
            }

            object IDummySync.Call(object param)
            {
                return "Call:" + param;
            }

            object IDummyExSync.CallEx(object param)
            {
                return "CallEx:" + param;
            }

            object IDummyEx2Sync.CallEx2(object param)
            {
                return "CallEx2:" + param;
            }

            object IDummyExFinalSync.CallExFinal(object param)
            {
                return "CallExFinal:" + param;
            }
        }

        public class DummyFinalExtendedActor : InterfacedActor, IExtendedInterface<IDummyExFinal>
        {
            public DummyFinalExtendedActor()
            {
            }

            [ExtendedHandler]
            private object Call(object param)
            {
                return "Call:" + param;
            }

            [ExtendedHandler]
            private object CallEx(object param)
            {
                return "CallEx:" + param;
            }

            [ExtendedHandler]
            private object CallEx2(object param)
            {
                return "CallEx2:" + param;
            }

            [ExtendedHandler]
            private Task<object> CallExFinal(object param)
            {
                return Task.FromResult<object>("CallExFinal:" + param);
            }
        }

        [Theory]
        [InlineData(typeof(DummyFinalActor))]
        [InlineData(typeof(DummyFinalSyncActor))]
        [InlineData(typeof(DummyFinalExtendedActor))]
        public async Task RequestInheritedInterfaced_Called(Type actorType)
        {
            // Arrange
            var a = new DummyExFinalRef(ActorOf(Props.Create(actorType)));

            // Act & Assert
            Assert.Equal("Call:1", await a.Call("1"));
            Assert.Equal("CallEx:1", await a.CallEx("1"));
            Assert.Equal("CallEx2:1", await a.CallEx2("1"));
            Assert.Equal("CallExFinal:1", await a.CallExFinal("1"));
        }
    }
}
