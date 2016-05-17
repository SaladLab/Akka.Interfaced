using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterAuthorizeAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter
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

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} Authorize.OnPreRequest");

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
    public sealed class TestFilterFirstLogAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter, IPostRequestFilter
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

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} FirstLog.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} FirstLog.OnPostRequest");
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterLastLogAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter, IPostRequestFilter
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

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} LastLog.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterPipeline.LogBoard.Log($"{_actorType.Name} LastLog.OnPostRequest");
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
        private void Atomic(int id)
        {
            TestFilterPipeline.LogBoard.Log($"TestFilterPipelineActor.Atomic {id}");
            if (id == 0)
                throw new ArgumentException("id");
        }

        [ExtendedHandler, Reentrant]
        private async Task Reentrant(int id)
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

        public TestFilterPipeline(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_PreRequestFilter_Normal()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPipelineActor>(Props.Create<TestFilterPipelineActor>(1));
            var a = new WorkerRef(actor);
            await a.Atomic(1);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPipelineActor FirstLog.OnPreRequest",
                    "TestFilterPipelineActor Authorize.OnPreRequest",
                    "TestFilterPipelineActor LastLog.OnPreRequest",
                    "TestFilterPipelineActor.Atomic 1",
                    "TestFilterPipelineActor LastLog.OnPostRequest",
                    "TestFilterPipelineActor FirstLog.OnPostRequest",
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_PreRequestFilter_Intercept()
        {
            var actor = ActorOfAsTestActorRef<TestFilterPipelineActor>(Props.Create<TestFilterPipelineActor>(0));
            var a = new WorkerRef(actor);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Atomic(1));

            Assert.Equal(
                new List<string>
                {
                    "TestFilterPipelineActor FirstLog.OnPreRequest",
                    "TestFilterPipelineActor Authorize.OnPreRequest",
                    "TestFilterPipelineActor LastLog.OnPostRequest",
                    "TestFilterPipelineActor FirstLog.OnPostRequest",
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
