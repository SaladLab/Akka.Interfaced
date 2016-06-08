using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class NotificationFilterOrderTest : TestKit.Xunit2.TestKit
    {
        public NotificationFilterOrderTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        // FilterFirst

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class NotificationFilterFirstAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new NotificationFilterFirstFilter(_actorType.Name + "_1");
        }

        public class NotificationFilterFirstFilter : IPreNotificationFilter, IPostNotificationFilter
        {
            private readonly string _name;

            public NotificationFilterFirstFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 1;

            void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        // FilterSecond

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class NotificationFilterSecondAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new NotificationFilterSecondFilter(_actorType.Name + "_2");
        }

        public class NotificationFilterSecondFilter : IPreNotificationFilter, IPostNotificationFilter
        {
            private readonly string _name;

            public NotificationFilterSecondFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 2;

            void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        public class TestFilterActor : InterfacedActor, ISubjectObserver
        {
            private LogBoard<string> _log;

            public TestFilterActor(LogBoard<string> log)
            {
                _log = log;
            }

            [NotificationFilterFirst, NotificationFilterSecond]
            void ISubjectObserver.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }
        }

        [Fact]
        public async Task FilterOrder_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = new SubjectRef(ActorOf(() => new SubjectActor()));
            var observingActor = ActorOf(() => new TestFilterActor(log));
            await subject.Subscribe(new SubjectObserver(observingActor));

            // Act
            await subject.MakeEvent("A");

            // Assert
            Assert.Equal(
                new[]
                {
                    "TestFilterActor_1.OnPreNotification",
                    "TestFilterActor_2.OnPreNotification",
                    "Event(A)",
                    "TestFilterActor_2.OnPostNotification",
                    "TestFilterActor_1.OnPostNotification"
                },
                log);
        }
    }
}
