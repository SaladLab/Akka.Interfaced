using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Akka.Actor;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterAuthorizeAttribute : Attribute, IFilterPerClassFactory, IPreHandleFilter
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

        int IFilter.Order => 1;

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} Authorize.OnPreHandle");

            var actor = (TestFilterPipelineActor)context.Actor;
            if (actor.Permission < 1)
            {
                context.Response = new ResponseMessage
                {
                    RequestId = context.Request.RequestId,
                    Exception = new InvalidOperationException("Not enought permission.")
                };
                return;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterFirstLogAttribute : Attribute, IFilterPerClassFactory, IPreHandleFilter, IPostHandleFilter
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

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} FirstLog.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} FirstLog.OnPostHandle");
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterLastLogAttribute : Attribute, IFilterPerClassFactory, IPreHandleFilter, IPostHandleFilter
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

        int IFilter.Order => 2;

        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} LastLog.OnPreHandle");
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} LastLog.OnPostHandle");
        }
    }

    [TestFilterAuthorize, TestFilterFirstLog, TestFilterLastLog]
    public class TestFilterPipelineActor : InterfacedActor<TestFilterPipelineActor>, IExtendedInterface<IWorker>
    {
        public int Permission { get; }

        public TestFilterPipelineActor(int permission)
        {
            Permission = permission;

        }

        [ExtendedHandler]
        void Atomic(int id)
        {
            TestFilterPipeline.LogBoard.Log($"TestFilterPipelineActor.Atomic {id}");
            if (id == 0)
                throw new ArgumentException("id");
        }

        [ExtendedHandler, Reentrant]
        async Task Reentrant(int id)
        {
            TestFilterPipeline.LogBoard.Log($"TestFilterPipelineActor.Reentrant {id}");
            if (id == 0)
                throw new ArgumentException("id");

            await Task.Yield();
            TestFilterPipeline.LogBoard.Log($"TestFilterPipelineActor.Reentrant Done {id}");
        }
    }

    public class TestFilterPipeline : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        [Fact]
        public async Task Test_PreHandleFilter_Normal()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPipelineActor>(Props.Create<TestFilterPipelineActor>(1));
            var a = new WorkerRef(actor);
            await a.Atomic(1);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPipelineActor FirstLog.OnPreHandle",
                    "TestFilterPipelineActor Authorize.OnPreHandle",
                    "TestFilterPipelineActor LastLog.OnPreHandle",
                    "TestFilterPipelineActor.Atomic 1",
                    "TestFilterPipelineActor LastLog.OnPostHandle",
                    "TestFilterPipelineActor FirstLog.OnPostHandle",
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_PreHandleFilter_Intercept()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPipelineActor>(Props.Create<TestFilterPipelineActor>(0));
            var a = new WorkerRef(actor);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Atomic(1));

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPipelineActor FirstLog.OnPreHandle",
                    "TestFilterPipelineActor Authorize.OnPreHandle",
                    "TestFilterPipelineActor LastLog.OnPostHandle",
                    "TestFilterPipelineActor FirstLog.OnPostHandle",
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
