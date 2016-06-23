using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class InterfacedActorOfTest : TestKit.Xunit2.TestKit
    {
        public InterfacedActorOfTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestDummyEx : InterfacedActor, IDummyEx
        {
            Task<object> IDummy.Call(object param) => Task.FromResult(param);
            Task<object> IDummyEx.CallEx(object param) => Task.FromResult(param);
        }

        public class TestDummyExSync : InterfacedActor, IDummyExSync
        {
            object IDummySync.Call(object param) => param;
            object IDummyExSync.CallEx(object param) => param;
        }

        public class TestDummyExExtended : InterfacedActor, IExtendedInterface<IDummyEx>
        {
            [ExtendedHandler] private object Call(object param) => param;
            [ExtendedHandler] private object CallEx(object param) => param;
        }

        [Theory]
        [InlineData(typeof(TestDummyEx))]
        [InlineData(typeof(TestDummyExSync))]
        [InlineData(typeof(TestDummyExExtended))]
        public void CheckIfActorImplements(Type actorType)
        {
            Assert.True(InterfacedActorOfExtensions.CheckIfActorImplements(actorType, typeof(IDummy)));
            Assert.True(InterfacedActorOfExtensions.CheckIfActorImplements(actorType, typeof(IDummyEx)));
            Assert.False(InterfacedActorOfExtensions.CheckIfActorImplements(actorType, typeof(IDummyEx2)));
            Assert.False(InterfacedActorOfExtensions.CheckIfActorImplements(actorType, typeof(IDummyExFinal)));
        }

        [Theory]
        [InlineData(typeof(TestDummyEx))]
        [InlineData(typeof(TestDummyExSync))]
        [InlineData(typeof(TestDummyExExtended))]
        public async Task CastTypedActorRefToInterfacedRef(Type actorType)
        {
            DummyExRef actor = Sys.InterfacedActorOf(Props.Create(actorType));
            Assert.Equal("Test", await actor.Call("Test"));
        }

        [Theory]
        [InlineData(typeof(TestDummyEx))]
        [InlineData(typeof(TestDummyExSync))]
        [InlineData(typeof(TestDummyExExtended))]
        public void CastTypedActorRefToWrongInterfacedRef(Type actorType)
        {
            var exception = Record.Exception(() =>
            {
                DummyEx2Ref actor = Sys.InterfacedActorOf(Props.Create(actorType));
            });
            Assert.IsType<InvalidCastException>(exception);
        }
    }
}
