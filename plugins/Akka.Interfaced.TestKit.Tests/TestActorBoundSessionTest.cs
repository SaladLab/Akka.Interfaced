using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.TestKit.Tests
{
    public class TestActorBoundSessionTest : Akka.TestKit.Xunit2.TestKit
    {
        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context)
        {
            return new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(() => new UserLoginActor(context.Self))),
                    typeof(IUserLogin))
            };
        }

        public TestActorBoundSessionTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_Bound_Succeed()
        {
            var actorBoundSession = ActorOfAsTestActorRef<TestActorBoundSession>(
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            var userLogin = new UserLoginRef(null, actorBoundSession.UnderlyingActor.GetRequestWaiter(1), null);
            var userActorId = await userLogin.Login("test", "test", 1);
            var user = new UserRef(null, actorBoundSession.UnderlyingActor.GetRequestWaiter(userActorId), null);

            Assert.Equal("test", await user.GetId());
        }

        [Fact]
        public async Task Test_MismatchedInterface_Fail()
        {
            var actorBoundSession = ActorOfAsTestActorRef<TestActorBoundSession>(
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            var user = new UserRef(null, actorBoundSession.UnderlyingActor.GetRequestWaiter(1), null);

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

            var user = new UserRef(null, actorBoundSession.UnderlyingActor.GetRequestWaiter(2), null);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await user.GetId();
            });
        }

        [Fact]
        public async Task Test_Observer_Succeed()
        {
            var actorBoundSession = ActorOfAsTestActorRef<TestActorBoundSession>(
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            var userLogin = new UserLoginRef(null, actorBoundSession.UnderlyingActor.GetRequestWaiter(1), null);
            var events = new List<IInvokable>();
            var observer = actorBoundSession.UnderlyingActor.AddTestObserver();
            observer.Notified += e => events.Add(e);
            var userActorId = await userLogin.Login("test", "test", observer.Id);
            var user = new UserRef(null, actorBoundSession.UnderlyingActor.GetRequestWaiter(userActorId), null);

            await user.Say("Hello");
            Assert.Equal("Hello", ((IUserObserver_PayloadTable.Say_Invoke)events[0]).message);
        }
    }
}
