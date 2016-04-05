using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;

namespace Akka.Interfaced.Tests
{
    public class BasicActor : InterfacedActor<BasicActor>, IBasic
    {
        private List<object> _blackhole;

        public BasicActor()
            : this(null)
        {
        }

        public BasicActor(List<object> blackhole)
        {
            _blackhole = blackhole;
        }

        Task IBasic.Call()
        {
            return Task.FromResult(0);
        }

        Task IBasic.CallWithParameter(int value)
        {
            _blackhole.Add(value);
            return Task.FromResult(0);
        }

        Task<int> IBasic.CallWithParameterAndReturn(int value)
        {
            return Task.FromResult(value);
        }

        Task<int> IBasic.CallWithReturn()
        {
            return Task.FromResult(1);
        }

        async Task<int> IBasic.ThrowException(bool throwException)
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
            var actor = ActorOfAsTestActorRef<BasicActor>();
            var a = new BasicRef(actor);
            await a.Call();
        }

        [Fact]
        public async Task Test_BasicActor_CallWithParameter()
        {
            var blackhole = new List<object>();
            var actor = ActorOfAsTestActorRef<BasicActor>(Props.Create<BasicActor>(blackhole));
            var a = new BasicRef(actor);
            await a.CallWithParameter(1);
            Assert.Equal(1, blackhole[0]);
        }

        [Fact]
        public async Task Test_BasicActor_CallWithParameterAndReturn()
        {
            var blackhole = new List<object>();
            var actor = ActorOfAsTestActorRef<BasicActor>();
            var a = new BasicRef(actor);
            var r = await a.CallWithParameterAndReturn(1);
            Assert.Equal(1, r);
        }

        [Fact]
        public async Task Test_BasicActor_CallWithReturn()
        {
            var actor = ActorOfAsTestActorRef<BasicActor>();
            var a = new BasicRef(actor);
            var r = await a.CallWithReturn();
            Assert.Equal(1, r);
        }

        [Fact]
        public async Task Test_BasicActor_ThrowException_Throw()
        {
            var actor = ActorOfAsTestActorRef<BasicActor>();
            var a = new BasicRef(actor);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await a.ThrowException(true);
            });
        }

        [Fact]
        public async Task Test_BasicActor_ThrowException_NoThrow()
        {
            var actor = ActorOfAsTestActorRef<BasicActor>();
            var a = new BasicRef(actor);
            var r = await a.ThrowException(false);
            Assert.Equal(1, r);
        }
    }
}
