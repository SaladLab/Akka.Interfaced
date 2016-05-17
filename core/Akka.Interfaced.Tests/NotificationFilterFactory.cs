using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

    public class NotificationFilterPerClassActor : InterfacedActor<NotificationFilterPerClassActor>, IDummy
    {
        [NotificationFilterPerClass]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
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

    public class NotificationFilterPerClassMethodActor : InterfacedActor<NotificationFilterPerClassMethodActor>, IDummy
    {
        [NotificationFilterPerClassMethod]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
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
    public class NotificationFilterPerInstanceActor : InterfacedActor<NotificationFilterPerInstanceActor>, IWorker
    {
        Task IWorker.Atomic(int id)
        {
            return Task.FromResult(0);
        }

        Task IWorker.Reentrant(int id)
        {
            return Task.FromResult(0);
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
    public class NotificationFilterPerInstanceMethodActor : InterfacedActor<NotificationFilterPerInstanceMethodActor>, IWorker
    {
        Task IWorker.Atomic(int id)
        {
            return Task.FromResult(0);
        }

        Task IWorker.Reentrant(int id)
        {
            return Task.FromResult(0);
        }
    }

    // FilterPerNotification

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class NotificationFilterPerNotificationAttribute : Attribute, IFilterPerNotificationFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerNotificationFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerNotificationFactory.CreateInstance(object actor, NotificationMessage request)
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
    public class NotificationFilterPerNotificationActor : InterfacedActor<NotificationFilterPerNotificationActor>, IWorker
    {
        Task IWorker.Atomic(int id)
        {
            return Task.FromResult(0);
        }

        Task IWorker.Reentrant(int id)
        {
            return Task.FromResult(0);
        }
    }

    public class NotificationFilterFactory : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public NotificationFilterFactory(ITestOutputHelper output)
            : base(output: output)
        {
        }

        /*
        [Fact]
        public async Task FilterPerClass_Work()
        {
            var actor = ActorOfAsTestActorRef<NotificationFilterPerClassActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerClassActor.OnPreNotification",
                    "NotificationFilterPerClassActor.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerClassMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<NotificationFilterPerClassMethodActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerClassMethodActor.Call.OnPreNotification",
                    "NotificationFilterPerClassMethodActor.Call.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerInstance_Work()
        {
            var actor = ActorOfAsTestActorRef<NotificationFilterPerInstanceActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Reentrant(2);

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerInstanceActor.Constructor",
                    "NotificationFilterPerInstanceActor.OnPreNotification",
                    "NotificationFilterPerInstanceActor.OnPostNotification",
                    "NotificationFilterPerInstanceActor.OnPreNotification",
                    "NotificationFilterPerInstanceActor.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerInstanceMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<NotificationFilterPerInstanceMethodActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerInstanceMethodActor.Atomic.Constructor",
                    "NotificationFilterPerInstanceMethodActor.Reentrant.Constructor",
                    "NotificationFilterPerInstanceMethodActor.Atomic.OnPreNotification",
                    "NotificationFilterPerInstanceMethodActor.Atomic.OnPostNotification",
                    "NotificationFilterPerInstanceMethodActor.Atomic.OnPreNotification",
                    "NotificationFilterPerInstanceMethodActor.Atomic.OnPostNotification",
                    "NotificationFilterPerInstanceMethodActor.Reentrant.OnPreNotification",
                    "NotificationFilterPerInstanceMethodActor.Reentrant.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerNotification_Work()
        {
            var actor = ActorOfAsTestActorRef<NotificationFilterPerNotificationActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "NotificationFilterPerNotificationActor.Atomic.Constructor",
                    "NotificationFilterPerNotificationActor.Atomic.OnPreNotification",
                    "NotificationFilterPerNotificationActor.Atomic.OnPostNotification",
                    "NotificationFilterPerNotificationActor.Atomic.Constructor",
                    "NotificationFilterPerNotificationActor.Atomic.OnPreNotification",
                    "NotificationFilterPerNotificationActor.Atomic.OnPostNotification",
                    "NotificationFilterPerNotificationActor.Reentrant.Constructor",
                    "NotificationFilterPerNotificationActor.Reentrant.OnPreNotification",
                    "NotificationFilterPerNotificationActor.Reentrant.OnPostNotification"
                },
                LogBoard.GetAndClearLogs());
        }
        */
    }
}
