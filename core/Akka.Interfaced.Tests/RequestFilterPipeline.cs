using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterAuthorizeAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter
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
            RequestFilterPipeline.LogBoard.Log($"{_actorType.Name} Authorize.OnPreRequest");

            var actor = (RequestFilterPipelineActor)context.Actor;
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
    public sealed class RequestFilterFirstLogAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter, IPostRequestFilter
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
            RequestFilterPipeline.LogBoard.Log($"{_actorType.Name} FirstLog.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterPipeline.LogBoard.Log($"{_actorType.Name} FirstLog.OnPostRequest");
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterLastLogAttribute : Attribute, IFilterPerClassFactory, IPreRequestFilter, IPostRequestFilter
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
            RequestFilterPipeline.LogBoard.Log($"{_actorType.Name} LastLog.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterPipeline.LogBoard.Log($"{_actorType.Name} LastLog.OnPostRequest");
        }
    }

    [RequestFilterAuthorize, RequestFilterFirstLog, RequestFilterLastLog]
    public class RequestFilterPipelineActor : InterfacedActor, IExtendedInterface<IWorker>
    {
        public int Permission { get; }

        public RequestFilterPipelineActor(int permission)
        {
            Permission = permission;
        }

        [ExtendedHandler]
        private void Atomic(int id)
        {
            RequestFilterPipeline.LogBoard.Log($"RequestFilterPipelineActor.Atomic {id}");
            if (id == 0)
                throw new ArgumentException("id");
        }

        [ExtendedHandler, Reentrant]
        private async Task Reentrant(int id)
        {
            RequestFilterPipeline.LogBoard.Log($"RequestFilterPipelineActor.Reentrant {id}");
            if (id == 0)
                throw new ArgumentException("id");

            await Task.Yield();
            RequestFilterPipeline.LogBoard.Log($"RequestFilterPipelineActor.Reentrant Done {id}");
        }
    }

    public class RequestFilterPipeline : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public RequestFilterPipeline(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task PreRequestFilter_Normal()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPipelineActor>(Props.Create<RequestFilterPipelineActor>(1));
            var a = new WorkerRef(actor);
            await a.Atomic(1);

            Assert.Equal(
                new[]
                {
                    "RequestFilterPipelineActor FirstLog.OnPreRequest",
                    "RequestFilterPipelineActor Authorize.OnPreRequest",
                    "RequestFilterPipelineActor LastLog.OnPreRequest",
                    "RequestFilterPipelineActor.Atomic 1",
                    "RequestFilterPipelineActor LastLog.OnPostRequest",
                    "RequestFilterPipelineActor FirstLog.OnPostRequest",
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task PreRequestFilter_Intercept()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterPipelineActor>(Props.Create<RequestFilterPipelineActor>(0));
            var a = new WorkerRef(actor);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Atomic(1));

            Assert.Equal(
                new[]
                {
                    "RequestFilterPipelineActor FirstLog.OnPreRequest",
                    "RequestFilterPipelineActor Authorize.OnPreRequest",
                    "RequestFilterPipelineActor LastLog.OnPostRequest",
                    "RequestFilterPipelineActor FirstLog.OnPostRequest",
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
