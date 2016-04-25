using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterAsyncAttribute : Attribute, IFilterPerClassFactory, IPreHandleAsyncFilter, IPostHandleAsyncFilter
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

        async Task IPreHandleAsyncFilter.OnPreHandleAsync(PreHandleFilterContext context)
        {
            TestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreHandleAsync");
            await Task.Yield();
            TestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreHandleAsync Done");
        }

        async Task IPostHandleAsyncFilter.OnPostHandleAsync(PostHandleFilterContext context)
        {
            TestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostHandleAsync");
            await Task.Yield();
            TestFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostHandleAsync Done");
        }
    }

    [TestFilterAsync]
    public class TestFilterAsyncActor : InterfacedActor<TestFilterAsyncActor>, IExtendedInterface<IWorker>
    {
        [ExtendedHandler]
        private void Atomic(int id)
        {
            TestFilterAsync.LogBoard.Log($"TestFilterAsyncActor.Atomic {id}");
        }

        [ExtendedHandler, Reentrant]
        private async Task Reentrant(int id)
        {
            TestFilterAsync.LogBoard.Log($"TestFilterAsyncActor.Reentrant {id}");
            await Task.Yield();
            TestFilterAsync.LogBoard.Log($"TestFilterAsyncActor.Reentrant Done {id}");
        }
    }

    public class TestFilterAsync : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public TestFilterAsync(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_SyncHandler_With_AsyncFilter_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterAsyncActor>();
            var a = new WorkerRef(actor);
            await a.Atomic(1);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterAsyncActor Async.OnPreHandleAsync",
                    "TestFilterAsyncActor Async.OnPreHandleAsync Done",
                    "TestFilterAsyncActor.Atomic 1",
                    "TestFilterAsyncActor Async.OnPostHandleAsync",
                    "TestFilterAsyncActor Async.OnPostHandleAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task Test_AsyncHandler_With_AsyncFilter_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterAsyncActor>();
            var a = new WorkerRef(actor);
            await a.Reentrant(1);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterAsyncActor Async.OnPreHandleAsync",
                    "TestFilterAsyncActor Async.OnPreHandleAsync Done",
                    "TestFilterAsyncActor.Reentrant 1",
                    "TestFilterAsyncActor.Reentrant Done 1",
                    "TestFilterAsyncActor Async.OnPostHandleAsync",
                    "TestFilterAsyncActor Async.OnPostHandleAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
