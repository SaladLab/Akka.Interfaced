using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class NotificationFilterAsyncTest : TestKit.Xunit2.TestKit
    {
        public NotificationFilterAsyncTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

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
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPreNotificationAsync");
                await Task.Yield();
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPreNotificationAsync Done");
            }

            async Task IPostNotificationAsyncFilter.OnPostNotificationAsync(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPostNotificationAsync");
                await Task.Yield();
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPostNotificationAsync Done");
            }
        }

        [NotificationFilterAsync]
        public class NotificationFilterAsyncActor : InterfacedActor, IExtendedInterface<ISubject2Observer>
        {
            private LogBoard<string> _log;

            public NotificationFilterAsyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            [ExtendedHandler]
            private void Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }

            [ExtendedHandler, Reentrant]
            private async Task Event2(string eventName)
            {
                _log.Add($"Event2({eventName})");
                await Task.Yield();
                _log.Add($"Event2({eventName}) Done");
            }
        }

        private async Task<Tuple<Subject2Ref, TestActorRef<TObservingActor>>> SetupActors2<TObservingActor>(LogBoard<string> log)
            where TObservingActor : ActorBase
        {
            var subject = ActorOfAsTestActorRef<Subject2Actor>("Subject").Cast<Subject2Ref>();
            var observingActor = ActorOfAsTestActorRef<TObservingActor>(Props.Create<TObservingActor>(log));
            await subject.Subscribe(new Subject2Observer(new AkkaReceiverNotificationChannel(observingActor)));
            return Tuple.Create(subject, observingActor);
        }

        [Fact]
        public async Task SyncHandler_With_AsyncFilter_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors2<NotificationFilterAsyncActor>(log);

            // Act
            await actors.Item1.MakeEvent("A");
            await actors.Item2.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(
                new[]
                {
                    "NotificationFilterAsyncActor Async.OnPreNotificationAsync",
                    "NotificationFilterAsyncActor Async.OnPreNotificationAsync Done",
                    "Event(A)",
                    "NotificationFilterAsyncActor Async.OnPostNotificationAsync",
                    "NotificationFilterAsyncActor Async.OnPostNotificationAsync Done"
                },
                log);
        }

        [Fact]
        public async Task AsyncHandler_With_AsyncFilter_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actors = await SetupActors2<NotificationFilterAsyncActor>(log);

            // Act
            await actors.Item1.MakeEvent2("A");
            await actors.Item2.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
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
                log);
        }
    }
}
