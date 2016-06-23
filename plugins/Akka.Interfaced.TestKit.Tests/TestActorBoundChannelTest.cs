using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.SlimServer;
using Akka.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.TestKit.Tests
{
    public class TestActorBoundChannelTest : Akka.TestKit.Xunit2.TestKit
    {
        private IActorRef _actorBoundChannelRef;
        private TestActorBoundChannel _actorBoundChannel;

        public TestActorBoundChannelTest(ITestOutputHelper output)
            : base(output: output)
        {
            InitializeActorBoundChannel();
        }

        private void InitializeActorBoundChannel()
        {
            var a = ActorOfAsTestActorRef<TestActorBoundChannel>(
                Props.Create(() => new TestActorBoundChannel(CreateInitialActor)));

            _actorBoundChannelRef = a;
            _actorBoundChannel = a.UnderlyingActor;
        }

        private Tuple<IActorRef, TaggedType[], ActorBindingFlags>[] CreateInitialActor(IActorContext context)
        {
            return new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(() => new UserLoginActor(context.Self))),
                    new TaggedType[] { typeof(IUserLogin) },
                    ActorBindingFlags.CloseThenStop)
            };
        }

        [Fact]
        public async Task Test_Bound_Succeed()
        {
            var userLogin = _actorBoundChannel.CreateRef<UserLoginRef>();
            var observer = _actorBoundChannel.CreateObserver<IUserObserver>(null);
            var user = await userLogin.Login("test", "test", observer);

            Assert.Equal("test", await user.GetId());
        }

        [Fact]
        public async Task Test_MismatchedInterface_Fail()
        {
            var actorBoundChannel = ActorOfAsTestActorRef<TestActorBoundChannel>(
                Props.Create(() => new TestActorBoundChannel(CreateInitialActor)));

            var user = _actorBoundChannel.CreateRef<UserRef>(1);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await user.GetId();
            });
        }

        [Fact]
        public async Task Test_NotBoundActor_Fail()
        {
            var actorBoundChannel = ActorOfAsTestActorRef<TestActorBoundChannel>(
                Props.Create(() => new TestActorBoundChannel(CreateInitialActor)));

            var user = _actorBoundChannel.CreateRef<UserRef>(2);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await user.GetId();
            });
        }

        private class TestObserver : IUserObserver
        {
            public List<string> Messages = new List<string>();

            public void Say(string message)
            {
                Messages.Add(message);
            }
        }

        [Fact]
        public async Task Test_Observer_Succeed()
        {
            var userLogin = _actorBoundChannel.CreateRef<UserLoginRef>();
            var observer = new TestObserver();

            var user = await userLogin.Login(
                "test", "test", _actorBoundChannel.CreateObserver<IUserObserver>(observer));
            await user.Say("Hello");

            Assert.Equal(new[] { "Hello" }, observer.Messages);
        }
    }
}
