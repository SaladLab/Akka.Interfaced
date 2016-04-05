using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

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

    public class TestFilterPerClassFilter : IPreHandleFilter, IPostHandleFilter
    {
        private readonly string _name;

        public TestFilterPerClassFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostHandle");
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

    public class TestFilterPerClassMethodFilter : IPreHandleFilter, IPostHandleFilter
    {
        private readonly string _name;

        public TestFilterPerClassMethodFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostHandle");
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

    public class TestFilterPerInstanceFilter : IPreHandleFilter, IPostHandleFilter
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

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostHandle");
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

    public class TestFilterPerInstanceMethodFilter : IPreHandleFilter, IPostHandleFilter
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

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostHandle");
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

    // FilterPerInvoke

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterPerInvokeAttribute : Attribute, IFilterPerInvokeFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerInvokeFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerInvokeFactory.CreateInstance(object actor, RequestMessage request)
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new TestFilterPerInvokeFilter(actor != null ? name : null);
        }
    }

    public class TestFilterPerInvokeFilter : IPreHandleFilter, IPostHandleFilter
    {
        private string _name;

        public TestFilterPerInvokeFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                TestFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterFactory.LogBoard.Log($"{_name}.OnPostHandle");
        }
    }

    [TestFilterPerInvoke]
    public class TestFilterPerInvokeActor : InterfacedActor<TestFilterPerInvokeActor>, IWorker
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

        [Fact]
        public async Task Test_FilterPerClass_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerClassActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerClassActor.OnPreHandle",
                    "TestFilterPerClassActor.OnPostHandle"
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
                    "TestFilterPerClassMethodActor.Call.OnPreHandle",
                    "TestFilterPerClassMethodActor.Call.OnPostHandle"
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
                    "TestFilterPerInstanceActor.OnPreHandle",
                    "TestFilterPerInstanceActor.OnPostHandle",
                    "TestFilterPerInstanceActor.OnPreHandle",
                    "TestFilterPerInstanceActor.OnPostHandle"
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
                    "TestFilterPerInstanceMethodActor.Atomic.OnPreHandle",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPostHandle",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPreHandle",
                    "TestFilterPerInstanceMethodActor.Atomic.OnPostHandle",
                    "TestFilterPerInstanceMethodActor.Reentrant.OnPreHandle",
                    "TestFilterPerInstanceMethodActor.Reentrant.OnPostHandle"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_FilterPerInvoke_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPerInvokeActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);
            await a.Atomic(2);
            await a.Reentrant(3);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPerInvokeActor.Atomic.Constructor",
                    "TestFilterPerInvokeActor.Atomic.OnPreHandle",
                    "TestFilterPerInvokeActor.Atomic.OnPostHandle",
                    "TestFilterPerInvokeActor.Atomic.Constructor",
                    "TestFilterPerInvokeActor.Atomic.OnPreHandle",
                    "TestFilterPerInvokeActor.Atomic.OnPostHandle",
                    "TestFilterPerInvokeActor.Reentrant.Constructor",
                    "TestFilterPerInvokeActor.Reentrant.OnPreHandle",
                    "TestFilterPerInvokeActor.Reentrant.OnPostHandle"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
