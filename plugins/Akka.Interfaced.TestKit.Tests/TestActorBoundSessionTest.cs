using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.TestKit.Tests
{
    public class TestActorBoundSessionTest : Akka.TestKit.Xunit2.TestKit
    {
        private IActorRef _actorBoundSessionRef;
        private TestActorBoundSession _actorBoundSession;

        public TestActorBoundSessionTest(ITestOutputHelper output)
            : base(output: output)
        {
            InitializeActorBoundSession();
        }

        private void InitializeActorBoundSession()
        {
            var a = ActorOfAsTestActorRef<TestActorBoundSession>(
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            _actorBoundSessionRef = a;
            _actorBoundSession = a.UnderlyingActor;
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context)
        {
            return new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(() => new UserLoginActor(context.Self))),
                    typeof(IUserLogin))
            };
        }

        [Fact]
        public async Task Test_Bound_Succeed()
        {
            var userLogin = _actorBoundSession.CreateRef<UserLoginRef>();
            var observer = _actorBoundSession.CreateObserver<IUserObserver>(null);
            var user = await userLogin.Login("test", "test", observer);

            Assert.Equal("test", await user.GetId());
        }

        [Fact]
        public async Task Test_MismatchedInterface_Fail()
        {
            var actorBoundSession = ActorOfAsTestActorRef<TestActorBoundSession>(
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            var user = _actorBoundSession.CreateRef<UserRef>(1);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await user.GetId();
            });
        }

        [Fact]
        public async Task Test_NotBoundActor_Fail()
        {
            var actorBoundSession = ActorOfAsTestActorRef<TestActorBoundSession>(
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            var user = _actorBoundSession.CreateRef<UserRef>(2);

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
            var userLogin = _actorBoundSession.CreateRef<UserLoginRef>();
            var observer = new TestObserver();

            var user = await userLogin.Login(
                "test", "test", _actorBoundSession.CreateObserver<IUserObserver>(observer));
            await user.Say("Hello");

            Assert.Equal(new[] { "Hello" }, observer.Messages);
        }
    }
}
