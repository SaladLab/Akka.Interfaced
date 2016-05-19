using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterAsyncAttribute
        : Attribute, IFilterPerClassFactory, IPreRequestAsyncFilter, IPostRequestAsyncFilter
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

        async Task IPreRequestAsyncFilter.OnPreRequestAsync(PreRequestFilterContext context)
        {
            RequestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreRequestAsync");
            await Task.Yield();
            RequestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreRequestAsync Done");
        }

        async Task IPostRequestAsyncFilter.OnPostRequestAsync(PostRequestFilterContext context)
        {
            RequestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostRequestAsync");
            await Task.Yield();
            RequestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostRequestAsync Done");
        }
    }

    [RequestFilterAsync]
    public class RequestFilterAsyncActor : InterfacedActor, IExtendedInterface<IWorker>
    {
        [ExtendedHandler]
        private void Atomic(int id)
        {
            RequestFilterAsync.LogBoard.Log($"Atomic {id}");
        }

        [ExtendedHandler, Reentrant]
        private async Task Reentrant(int id)
        {
            RequestFilterAsync.LogBoard.Log($"Reentrant {id}");
            await Task.Yield();
            RequestFilterAsync.LogBoard.Log($"Reentrant Done {id}");
        }
    }

    public class RequestFilterAsync : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public RequestFilterAsync(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task SyncHandler_With_AsyncFilter_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterAsyncActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);

            Assert.Equal(
                new[]
                {
                    "RequestFilterAsyncActor Async.OnPreRequestAsync",
                    "RequestFilterAsyncActor Async.OnPreRequestAsync Done",
                    "Atomic 1",
                    "RequestFilterAsyncActor Async.OnPostRequestAsync",
                    "RequestFilterAsyncActor Async.OnPostRequestAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task AsyncHandler_With_AsyncFilter_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterAsyncActor>();
            var a = new WorkerRef(actor);
            await a.Reentrant(1);

            Assert.Equal(
                new[]
                {
                    "RequestFilterAsyncActor Async.OnPreRequestAsync",
                    "RequestFilterAsyncActor Async.OnPreRequestAsync Done",
                    "Reentrant 1",
                    "Reentrant Done 1",
                    "RequestFilterAsyncActor Async.OnPostRequestAsync",
                    "RequestFilterAsyncActor Async.OnPostRequestAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
