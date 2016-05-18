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
    public sealed class RequestFilterPerClassAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new RequestFilterPerClassFilter(_actorType.Name);
        }
    }

    public class RequestFilterPerClassFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _name;

        public RequestFilterPerClassFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    public class RequestFilterPerClassActor : InterfacedActor<RequestFilterPerClassActor>, IDummy
    {
        [RequestFilterPerClass]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }
    }

    // FilterPerClassMethod

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterPerClassMethodAttribute : Attribute, IFilterPerClassMethodFactory
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
            return new RequestFilterPerClassMethodFilter(name);
        }
    }

    public class RequestFilterPerClassMethodFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _name;

        public RequestFilterPerClassMethodFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    public class RequestFilterPerClassMethodActor : InterfacedActor<RequestFilterPerClassMethodActor>, IDummy
    {
        [RequestFilterPerClassMethod]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }
    }

    // FilterPerInstance

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterPerInstanceAttribute : Attribute, IFilterPerInstanceFactory
    {
        private Type _actorType;

        void IFilterPerInstanceFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerInstanceFactory.CreateInstance(object actor)
        {
            return new RequestFilterPerInstanceFilter(actor != null ? _actorType.Name : null);
        }
    }

    public class RequestFilterPerInstanceFilter : IPreRequestFilter, IPostRequestFilter
    {
        private string _name;

        public RequestFilterPerInstanceFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                RequestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    [RequestFilterPerInstance]
    public class RequestFilterPerInstanceActor : InterfacedActor<RequestFilterPerInstanceActor>, IWorker
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
    public sealed class RequestFilterPerInstanceMethodAttribute : Attribute, IFilterPerInstanceMethodFactory
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
            return new RequestFilterPerInstanceMethodFilter(actor != null ? name : null);
        }
    }

    public class RequestFilterPerInstanceMethodFilter : IPreRequestFilter, IPostRequestFilter
    {
        private string _name;

        public RequestFilterPerInstanceMethodFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                RequestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    [RequestFilterPerInstanceMethod]
    public class RequestFilterPerInstanceMethodActor : InterfacedActor<RequestFilterPerInstanceMethodActor>, IWorker
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

    // FilterPerRequest

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterPerRequestAttribute : Attribute, IFilterPerRequestFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerRequestFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerRequestFactory.CreateInstance(object actor, RequestMessage request)
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new RequestFilterPerRequestFilter(actor != null ? name : null);
        }
    }

    public class RequestFilterPerRequestFilter : IPreRequestFilter, IPostRequestFilter
    {
        private string _name;

        public RequestFilterPerRequestFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                RequestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    [RequestFilterPerRequest]
    public class RequestFilterPerRequestActor : InterfacedActor<RequestFilterPerRequestActor>, IWorker
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

    public class RequestFilterFactory : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard;

        public RequestFilterFactory(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new FilterLogBoard();
        }

        [Fact]
        public async Task FilterPerClass_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPerClassActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "RequestFilterPerClassActor.OnPreRequest",
                    "RequestFilterPerClassActor.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerClassMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPerClassMethodActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "RequestFilterPerClassMethodActor.Call.OnPreRequest",
                    "RequestFilterPerClassMethodActor.Call.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerInstance_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPerInstanceActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Reentrant(2);

            Assert.Equal(
                new List<string>
                {
                    "RequestFilterPerInstanceActor.Constructor",
                    "RequestFilterPerInstanceActor.OnPreRequest",
                    "RequestFilterPerInstanceActor.OnPostRequest",
                    "RequestFilterPerInstanceActor.OnPreRequest",
                    "RequestFilterPerInstanceActor.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerInstanceMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPerInstanceMethodActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "RequestFilterPerInstanceMethodActor.Atomic.Constructor",
                    "RequestFilterPerInstanceMethodActor.Reentrant.Constructor",
                    "RequestFilterPerInstanceMethodActor.Atomic.OnPreRequest",
                    "RequestFilterPerInstanceMethodActor.Atomic.OnPostRequest",
                    "RequestFilterPerInstanceMethodActor.Atomic.OnPreRequest",
                    "RequestFilterPerInstanceMethodActor.Atomic.OnPostRequest",
                    "RequestFilterPerInstanceMethodActor.Reentrant.OnPreRequest",
                    "RequestFilterPerInstanceMethodActor.Reentrant.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task FilterPerRequest_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPerRequestActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "RequestFilterPerRequestActor.Atomic.Constructor",
                    "RequestFilterPerRequestActor.Atomic.OnPreRequest",
                    "RequestFilterPerRequestActor.Atomic.OnPostRequest",
                    "RequestFilterPerRequestActor.Atomic.Constructor",
                    "RequestFilterPerRequestActor.Atomic.OnPreRequest",
                    "RequestFilterPerRequestActor.Atomic.OnPostRequest",
                    "RequestFilterPerRequestActor.Reentrant.Constructor",
                    "RequestFilterPerRequestActor.Reentrant.OnPreRequest",
                    "RequestFilterPerRequestActor.Reentrant.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
