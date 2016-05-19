using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterAsyncAttribute
        : Attribute, IFilterPerClassFactory, IPreNotificationAsyncFilter, IPostNotificationAsyncFilter
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return this;
        }

        int IFilter.Order => 0;

        async Task IPreNotificationAsyncFilter.OnPreNotificationAsync(PreNotificationFilterContext context)
        {
            NotificationFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreNotificationAsync");
            await Task.Yield();
            NotificationFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreNotificationAsync Done");
        }

        async Task IPostNotificationAsyncFilter.OnPostNotificationAsync(PostNotificationFilterContext context)
        {
            NotificationFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostNotificationAsync");
            await Task.Yield();
            NotificationFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostNotificationAsync Done");
        }
    }

    [NotificationFilterAsync]
    public class NotificationFilterAsyncActor : InterfacedActor, IExtendedInterface<ISubject2Observer>
    {
        [ExtendedHandler]
        private void Event(string eventName)
        {
            NotificationFilterAsync.LogBoard.Log($"Event({eventName})");
        }

        [ExtendedHandler, Reentrant]
        private async Task Event2(string eventName)
        {
            NotificationFilterAsync.LogBoard.Log($"Event2({eventName})");
            await Task.Yield();
            NotificationFilterAsync.LogBoard.Log($"Event2({eventName}) Done");
        }
    }

    public class NotificationFilterAsync : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard;

        public NotificationFilterAsync(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new FilterLogBoard();
        }

        private async Task<Subject2Ref> SetupActors2<TObservingActor>()
            where TObservingActor : ActorBase, new()
        {
            var subjectActor = ActorOfAsTestActorRef<Subject2Actor>("Subject");
            var subject = new Subject2Ref(subjectActor);
            var observingActor = ActorOfAsTestActorRef<TObservingActor>();
            await subject.Subscribe(new Subject2Observer(observingActor));
            return subject;
        }

        [Fact]
        public async Task SyncHandler_With_AsyncFilter_Work()
        {
            var subject = await SetupActors2<NotificationFilterAsyncActor>();
            await subject.MakeEvent("A");
            await subject.Actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            Assert.Equal(
                new[]
                {
                    "NotificationFilterAsyncActor Async.OnPreNotificationAsync",
                    "NotificationFilterAsyncActor Async.OnPreNotificationAsync Done",
                    "Event(A)",
                    "NotificationFilterAsyncActor Async.OnPostNotificationAsync",
                    "NotificationFilterAsyncActor Async.OnPostNotificationAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task AsyncHandler_With_AsyncFilter_Work()
        {
            var subject = await SetupActors2<NotificationFilterAsyncActor>();
            await subject.MakeEvent2("A");
            await subject.Actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            Assert.Equal(
                new[]
                {
                    "NotificationFilterAsyncActor Async.OnPreNotificationAsync",
                    "NotificationFilterAsyncActor Async.OnPreNotificationAsync Done",
                    "Event2(A)",
                    "Event2(A) Done",
                    "NotificationFilterAsyncActor Async.OnPostNotificationAsync",
                    "NotificationFilterAsyncActor Async.OnPostNotificationAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
