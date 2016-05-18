using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    // FilterPerClass

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterPerClassAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new NotificationFilterPerClassFilter(_actorType.Name);
        }
    }

    public class NotificationFilterPerClassFilter : IPreNotificationFilter, IPostNotificationFilter
    {
        private readonly string _name;

        public NotificationFilterPerClassFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPostNotification");
        }
    }

    public class NotificationFilterPerClassActor : InterfacedActor<NotificationFilterPerClassActor>, ISubjectObserver
    {
        [NotificationFilterPerClass]
        void ISubjectObserver.Event(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event({eventName})");
        }
    }

    // FilterPerClassMethod

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterPerClassMethodAttribute : Attribute, IFilterPerClassMethodFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerClassMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerClassMethodFactory.CreateInstance()
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new NotificationFilterPerClassMethodFilter(name);
        }
    }

    public class NotificationFilterPerClassMethodFilter : IPreNotificationFilter, IPostNotificationFilter
    {
        private readonly string _name;

        public NotificationFilterPerClassMethodFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPostNotification");
        }
    }

    public class NotificationFilterPerClassMethodActor : InterfacedActor<NotificationFilterPerClassMethodActor>, ISubjectObserver
    {
        [NotificationFilterPerClassMethod]
        void ISubjectObserver.Event(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event({eventName})");
        }
    }

    // FilterPerInstance

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterPerInstanceAttribute : Attribute, IFilterPerInstanceFactory
    {
        private Type _actorType;

        void IFilterPerInstanceFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerInstanceFactory.CreateInstance(object actor)
        {
            return new NotificationFilterPerInstanceFilter(actor != null ? _actorType.Name : null);
        }
    }

    public class NotificationFilterPerInstanceFilter : IPreNotificationFilter, IPostNotificationFilter
    {
        private string _name;

        public NotificationFilterPerInstanceFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                NotificationFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPostNotification");
        }
    }

    [NotificationFilterPerInstance]
    public class NotificationFilterPerInstanceActor : InterfacedActor<NotificationFilterPerInstanceActor>, ISubject2Observer
    {
        void ISubject2Observer.Event(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event({eventName})");
        }

        void ISubject2Observer.Event2(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event2({eventName})");
        }
    }

    // FilterPerInstanceMethod

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterPerInstanceMethodAttribute : Attribute, IFilterPerInstanceMethodFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerInstanceMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerInstanceMethodFactory.CreateInstance(object actor)
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new NotificationFilterPerInstanceMethodFilter(actor != null ? name : null);
        }
    }

    public class NotificationFilterPerInstanceMethodFilter : IPreNotificationFilter, IPostNotificationFilter
    {
        private string _name;

        public NotificationFilterPerInstanceMethodFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                NotificationFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPostNotification");
        }
    }

    [NotificationFilterPerInstanceMethod]
    public class NotificationFilterPerInstanceMethodActor : InterfacedActor<NotificationFilterPerInstanceMethodActor>, ISubject2Observer
    {
        void ISubject2Observer.Event(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event({eventName})");
        }

        void ISubject2Observer.Event2(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event2({eventName})");
        }
    }

    // FilterPerNotification

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterPerNotificationAttribute : Attribute, IFilterPerInvokeFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerInvokeFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerInvokeFactory.CreateInstance(object actor, object message)
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new NotificationFilterPerNotificationFilter(actor != null ? name : null);
        }
    }

    public class NotificationFilterPerNotificationFilter : IPreNotificationFilter, IPostNotificationFilter
    {
        private string _name;

        public NotificationFilterPerNotificationFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                NotificationFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPreNotification");
        }

        void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
        {
            NotificationFilterFactory.LogBoard.Log($"{_name}.OnPostNotification");
        }
    }

    [NotificationFilterPerNotification]
    public class NotificationFilterPerNotificationActor : InterfacedActor<NotificationFilterPerNotificationActor>, ISubjectObserver
    {
        void ISubjectObserver.Event(string eventName)
        {
            NotificationFilterFactory.LogBoard.Log($"Event({eventName})");
        }
    }

    public class NotificationFilterFactory : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard;

        public NotificationFilterFactory(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new FilterLogBoard();
        }

        private async Task<SubjectRef> SetupActors<TObservingActor>()
            where TObservingActor : ActorBase, new()
        {
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = new SubjectRef(subjectActor);
            var observingActor = ActorOfAsTestActorRef<TObservingActor>();
            await subject.Subscribe(new SubjectObserver(observingActor));
            return subject;
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
        public async Task FilterPerClass_Work()
        {
            var subject = await SetupActors<NotificationFilterPerClassActor>();
            await subject.MakeEvent("A");

            Assert.Equal(
                new[]
                {
                    "NotificationFilterPerClassActor.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerClassActor.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerClassMethod_Work()
        {
            var subject = await SetupActors<NotificationFilterPerClassMethodActor>();
            await subject.MakeEvent("A");

            Assert.Equal(
                new[]
                {
                    "NotificationFilterPerClassMethodActor.Event.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerClassMethodActor.Event.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerInstance_Work()
        {
            var subject = await SetupActors2<NotificationFilterPerInstanceActor>();
            await subject.MakeEvent("A");
            await subject.MakeEvent2("B");

            Assert.Equal(
                new[]
                {
                    "NotificationFilterPerInstanceActor.Constructor",
                    "NotificationFilterPerInstanceActor.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerInstanceActor.OnPostNotification",
                    "NotificationFilterPerInstanceActor.OnPreNotification",
                    "Event2(B)",
                    "NotificationFilterPerInstanceActor.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerInstanceMethod_Work()
        {
            var subject = await SetupActors2<NotificationFilterPerInstanceMethodActor>();
            await subject.MakeEvent("A");
            await subject.MakeEvent2("B");

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerInstanceMethodActor.Event.Constructor",
                    "NotificationFilterPerInstanceMethodActor.Event2.Constructor",
                    "NotificationFilterPerInstanceMethodActor.Event.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerInstanceMethodActor.Event.OnPostNotification",
                    "NotificationFilterPerInstanceMethodActor.Event2.OnPreNotification",
                    "Event2(B)",
                    "NotificationFilterPerInstanceMethodActor.Event2.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerNotification_Work()
        {
            var subject = await SetupActors<NotificationFilterPerNotificationActor>();
            await subject.MakeEvent("A");
            await subject.MakeEvent("B");

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerNotificationActor.Event.Constructor",
                    "NotificationFilterPerNotificationActor.Event.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerNotificationActor.Event.OnPostNotification",
                    "NotificationFilterPerNotificationActor.Event.Constructor",
                    "NotificationFilterPerNotificationActor.Event.OnPreNotification",
                    "Event(B)",
                    "NotificationFilterPerNotificationActor.Event.OnPostNotification",
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
