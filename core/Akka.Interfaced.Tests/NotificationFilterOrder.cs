using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    // FilterFirst

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterFirstAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new NotificationFilterFirstFilter(_actorType.Name + "_1");
        }
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
            NotificationFilterOrder.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterOrder.LogBoard.Log($"{_name}.OnPostNotification");
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

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new NotificationFilterSecondFilter(_actorType.Name + "_2");
        }
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
            NotificationFilterOrder.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterOrder.LogBoard.Log($"{_name}.OnPostNotification");
        }
    }

    public class NotificationFilterOrderActor : InterfacedActor, ISubjectObserver
    {
        [NotificationFilterFirst, NotificationFilterSecond]
        void ISubjectObserver.Event(string eventName)
        {
            NotificationFilterOrder.LogBoard.Log($"Event({eventName})");
        }
    }

    public class NotificationFilterOrder : Akka.TestKit.Xunit2.TestKit
    {
        public static LogBoard LogBoard;

        public NotificationFilterOrder(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new LogBoard();
        }

        [Fact]
        public async Task FilterOrder_Work()
        {
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = new SubjectRef(subjectActor);
            var observingActor = ActorOfAsTestActorRef<NotificationFilterOrderActor>();
            await subject.Subscribe(new SubjectObserver(observingActor));
            await subject.MakeEvent("A");

            Assert.Equal(
                new[]
                {
                    "NotificationFilterOrderActor_1.OnPreNotification",
                    "NotificationFilterOrderActor_2.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterOrderActor_2.OnPostNotification",
                    "NotificationFilterOrderActor_1.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
