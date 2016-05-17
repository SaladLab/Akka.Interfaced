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
    public sealed class TestFilterPerClassAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new TestFilterPerClassFilter(_actorType.Name);
        }
    }

    public class TestFilterPerClassFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _name;

        public TestFilterPerClassFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    public class TestFilterPerClassActor : InterfacedActor<TestFilterPerClassActor>, IDummy
    {
        [TestFilterPerClass]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }
    }

    // FilterPerClassMethod

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterPerClassMethodAttribute : Attribute, IFilterPerClassMethodFactory
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
            return new TestFilterPerClassMethodFilter(name);
        }
    }

    public class TestFilterPerClassMethodFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _name;

        public TestFilterPerClassMethodFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    public class TestFilterPerClassMethodActor : InterfacedActor<TestFilterPerClassMethodActor>, IDummy
    {
        [TestFilterPerClassMethod]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }
    }

    // FilterPerInstance

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterPerInstanceAttribute : Attribute, IFilterPerInstanceFactory
    {
        private Type _actorType;

        void IFilterPerInstanceFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerInstanceFactory.CreateInstance(object actor)
        {
            return new TestFilterPerInstanceFilter(actor != null ? _actorType.Name : null);
        }
    }

    public class TestFilterPerInstanceFilter : IPreRequestFilter, IPostRequestFilter
    {
        private string _name;

        public TestFilterPerInstanceFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                TestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    [TestFilterPerInstance]
    public class TestFilterPerInstanceActor : InterfacedActor<TestFilterPerInstanceActor>, IWorker
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
    public sealed class TestFilterPerInstanceMethodAttribute : Attribute, IFilterPerInstanceMethodFactory
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
            return new TestFilterPerInstanceMethodFilter(actor != null ? name : null);
        }
    }

    public class TestFilterPerInstanceMethodFilter : IPreRequestFilter, IPostRequestFilter
    {
        private string _name;

        public TestFilterPerInstanceMethodFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                TestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    [TestFilterPerInstanceMethod]
    public class TestFilterPerInstanceMethodActor : InterfacedActor<TestFilterPerInstanceMethodActor>, IWorker
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
    public sealed class TestFilterPerRequestAttribute : Attribute, IFilterPerRequestFactory
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
            return new TestFilterPerRequestFilter(actor != null ? name : null);
        }
    }

    public class TestFilterPerRequestFilter : IPreRequestFilter, IPostRequestFilter
    {
        private string _name;

        public TestFilterPerRequestFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                TestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    [TestFilterPerRequest]
    public class TestFilterPerRequestActor : InterfacedActor<TestFilterPerRequestActor>, IWorker
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

    public class TestFilterFactory : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public TestFilterFactory(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_FilterPerClass_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerClassActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerClassActor.OnPreRequest",
                    "TestFilterPerClassActor.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_FilterPerClassMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerClassMethodActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerClassMethodActor.Call.OnPreRequest",
                    "TestFilterPerClassMethodActor.Call.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_FilterPerInstance_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerInstanceActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Reentrant(2);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerInstanceActor.Constructor",
                    "TestFilterPerInstanceActor.OnPreRequest",
                    "TestFilterPerInstanceActor.OnPostRequest",
                    "TestFilterPerInstanceActor.OnPreRequest",
                    "TestFilterPerInstanceActor.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_FilterPerInstanceMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerInstanceMethodActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerInstanceMethodActor.Atomic.Constructor",
                    "TestFilterPerInstanceMethodActor.Reentrant.Constructor",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPreRequest",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPostRequest",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPreRequest",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPostRequest",
                    "TestFilterPerInstanceMethodActor.Reentrant.OnPreRequest",
                    "TestFilterPerInstanceMethodActor.Reentrant.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_FilterPerRequest_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerRequestActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerRequestActor.Atomic.Constructor",
                    "TestFilterPerRequestActor.Atomic.OnPreRequest",
                    "TestFilterPerRequestActor.Atomic.OnPostRequest",
                    "TestFilterPerRequestActor.Atomic.Constructor",
                    "TestFilterPerRequestActor.Atomic.OnPreRequest",
                    "TestFilterPerRequestActor.Atomic.OnPostRequest",
                    "TestFilterPerRequestActor.Reentrant.Constructor",
                    "TestFilterPerRequestActor.Reentrant.OnPreRequest",
                    "TestFilterPerRequestActor.Reentrant.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
