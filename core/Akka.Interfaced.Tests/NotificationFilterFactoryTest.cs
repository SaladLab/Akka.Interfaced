using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class NotificationFilterFactoryTest : TestKit.Xunit2.TestKit
    {
        public NotificationFilterFactoryTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        private async Task<SubjectRef> SetupActors<TObservingActor>(LogBoard<string> log)
            where TObservingActor : ActorBase
        {
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("Subject");
            var subject = subjectActor.Cast<SubjectRef>();
            var observingActor = ActorOfAsTestActorRef<TObservingActor>(Props.Create<TObservingActor>(log));
            await subject.Subscribe(new SubjectObserver(new AkkaReceiverNotificationChannel(observingActor)));
            return subject;
        }

        private async Task<Subject2Ref> SetupActors2<TObservingActor>(LogBoard<string> log)
            where TObservingActor : ActorBase
        {
            var subjectActor = ActorOfAsTestActorRef<Subject2Actor>("Subject");
            var subject = subjectActor.Cast<Subject2Ref>();
            var observingActor = ActorOfAsTestActorRef<TObservingActor>(Props.Create<TObservingActor>(log));
            await subject.Subscribe(new Subject2Observer(new AkkaReceiverNotificationChannel(observingActor)));
            return subject;
        }

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
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        public class NotificationFilterPerClassActor : InterfacedActor, ISubjectObserver
        {
            private LogBoard<string> _log;

            public NotificationFilterPerClassActor(LogBoard<string> log)
            {
                _log = log;
            }

            [NotificationFilterPerClass]
            void ISubjectObserver.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }
        }

        [Fact]
        public async Task FilterPerClass_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = await SetupActors<NotificationFilterPerClassActor>(log);

            // Act
            await subject.MakeEvent("A");

            // Assert
            Assert.Equal(
                new[]
                {
                    "NotificationFilterPerClassActor.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerClassActor.OnPostNotification"
                },
                log);
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
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        public class NotificationFilterPerClassMethodActor : InterfacedActor, ISubjectObserver
        {
            private LogBoard<string> _log;

            public NotificationFilterPerClassMethodActor(LogBoard<string> log)
            {
                _log = log;
            }

            [NotificationFilterPerClassMethod]
            void ISubjectObserver.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }
        }

        [Fact]
        public async Task FilterPerClassMethod_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = await SetupActors<NotificationFilterPerClassMethodActor>(log);

            // Act
            await subject.MakeEvent("A");

            // Assert
            Assert.Equal(
                new[]
                {
                    "NotificationFilterPerClassMethodActor.Event.OnPreNotification",
                    "Event(A)",
                    "NotificationFilterPerClassMethodActor.Event.OnPostNotification"
                },
                log);
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
                return new NotificationFilterPerInstanceFilter(actor != null ? _actorType.Name : null, actor);
            }
        }

        public class NotificationFilterPerInstanceFilter : IPreNotificationFilter, IPostNotificationFilter
        {
            private string _name;

            public NotificationFilterPerInstanceFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        [NotificationFilterPerInstance]
        public class NotificationFilterPerInstanceActor : InterfacedActor, ISubject2Observer
        {
            private LogBoard<string> _log;

            public NotificationFilterPerInstanceActor(LogBoard<string> log)
            {
                _log = log;
            }

            void ISubject2Observer.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }

            void ISubject2Observer.Event2(string eventName)
            {
                _log.Add($"Event2({eventName})");
            }
        }

        [Fact]
        public async Task FilterPerInstance_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = await SetupActors2<NotificationFilterPerInstanceActor>(log);

            // Act
            await subject.MakeEvent("A");
            await subject.MakeEvent2("B");

            // Assert
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
                log);
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
                return new NotificationFilterPerInstanceMethodFilter(actor != null ? name : null, actor);
            }
        }

        public class NotificationFilterPerInstanceMethodFilter : IPreNotificationFilter, IPostNotificationFilter
        {
            private string _name;

            public NotificationFilterPerInstanceMethodFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        [NotificationFilterPerInstanceMethod]
        public class NotificationFilterPerInstanceMethodActor : InterfacedActor, ISubject2Observer
        {
            private LogBoard<string> _log;

            public NotificationFilterPerInstanceMethodActor(LogBoard<string> log)
            {
                _log = log;
            }

            void ISubject2Observer.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }

            void ISubject2Observer.Event2(string eventName)
            {
                _log.Add($"Event2({eventName})");
            }
        }

        [Fact]
        public async Task FilterPerInstanceMethod_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = await SetupActors2<NotificationFilterPerInstanceMethodActor>(log);

            // Act
            await subject.MakeEvent("A");
            await subject.MakeEvent2("B");

            // Assert
            Assert.Equal(
                new[]
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
                log);
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
                return new NotificationFilterPerNotificationFilter(actor != null ? name : null, actor);
            }
        }

        public class NotificationFilterPerNotificationFilter : IPreNotificationFilter, IPostNotificationFilter
        {
            private string _name;

            public NotificationFilterPerNotificationFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreNotificationFilter.OnPreNotification(PreNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreNotification");
            }

            void IPostNotificationFilter.OnPostNotification(PostNotificationFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostNotification");
            }
        }

        [NotificationFilterPerNotification]
        public class NotificationFilterPerNotificationActor : InterfacedActor, ISubjectObserver
        {
            private LogBoard<string> _log;

            public NotificationFilterPerNotificationActor(LogBoard<string> log)
            {
                _log = log;
            }

            void ISubjectObserver.Event(string eventName)
            {
                _log.Add($"Event({eventName})");
            }
        }

        [Fact]
        public async Task FilterPerNotification_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = await SetupActors<NotificationFilterPerNotificationActor>(log);

            // Act
            await subject.MakeEvent("A");
            await subject.MakeEvent("B");

            // Assert
            Assert.Equal(
                new[]
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
                log);
        }
    }
}
