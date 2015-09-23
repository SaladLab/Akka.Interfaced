using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using System;

namespace Akka.Interfaced.Tests
{
    public class SimpleActor : InterfacedActor<SimpleActor>, ISimple
    {
        private List<object> _blackhole;

        public SimpleActor() : this(null) { }

        public SimpleActor(List<object> blackhole)
        {
            _blackhole = blackhole;
        }

        Task ISimple.Call()
        {
            return Task.FromResult(0);
        }

        Task ISimple.CallWithParameter(int value)
        {
            _blackhole.Add(value);
            return Task.FromResult(0);
        }

        Task<int> ISimple.CallWithParameterAndReturn(int value)
        {
            return Task.FromResult(value);
        }

        Task<int> ISimple.CallWithReturn()
        {
            return Task.FromResult(1);
        }

        async Task<int> ISimple.ThrowException(bool throwException)
        {
            if (throwException)
                throw new ArgumentException("throwException");

            await Task.Yield();
            return 1;
        }
    }

    public class TestBasic : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public async Task Test_BasicActor_Call()
        {
            var actor = ActorOfAsTestActorRef<SimpleActor>();
            var a = new SimpleRef(actor);
            await a.Call();
        }

        [Fact]
        public async Task Test_BasicActor_CallWithParameter()
        {
            var blackhole = new List<object>();
            var actor = ActorOfAsTestActorRef<SimpleActor>(Props.Create<SimpleActor>(blackhole));
            var a = new SimpleRef(actor);
            await a.CallWithParameter(1);
            Assert.Equal(1, blackhole[0]);
        }

        [Fact]
        public async Task Test_BasicActor_CallWithParameterAndReturn()
        {
            var blackhole = new List<object>();
            var actor = ActorOfAsTestActorRef<SimpleActor>();
            var a = new SimpleRef(actor);
            var r = await a.CallWithParameterAndReturn(1);
            Assert.Equal(1, r);
        }

        [Fact]
        public async Task Test_BasicActor_CallWithReturn()
        {
            var actor = ActorOfAsTestActorRef<SimpleActor>();
            var a = new SimpleRef(actor);
            var r = await a.CallWithReturn();
            Assert.Equal(1, r);
        }

        [Fact]
        public async Task Test_BasicActor_ThrowException_Throw()
        {
            var actor = ActorOfAsTestActorRef<SimpleActor>();
            var a = new SimpleRef(actor);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await a.ThrowException(true);
            });
        }

        [Fact]
        public async Task Test_BasicActor_ThrowException_NoThrow()
        {
            var actor = ActorOfAsTestActorRef<SimpleActor>();
            var a = new SimpleRef(actor);
            var r = await a.ThrowException(false);
            Assert.Equal(1, r);
        }
    }
}
