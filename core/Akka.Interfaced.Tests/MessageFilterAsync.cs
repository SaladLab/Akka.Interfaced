using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterAsyncAttribute
        : Attribute, IFilterPerClassFactory, IPreMessageAsyncFilter, IPostMessageAsyncFilter
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

        async Task IPreMessageAsyncFilter.OnPreMessageAsync(PreMessageFilterContext context)
        {
            MessageFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreMessageAsync");
            await Task.Yield();
            MessageFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPreMessageAsync Done");
        }

        async Task IPostMessageAsyncFilter.OnPostMessageAsync(PostMessageFilterContext context)
        {
            MessageFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostMessageAsync");
            await Task.Yield();
            MessageFilterAsync.LogBoard.Log($"{_actorType.Name} Async.OnPostMessageAsync Done");
        }
    }

    [MessageFilterAsync]
    public class MessageFilterAsyncActor : InterfacedActor<MessageFilterAsyncActor>
    {
        [MessageHandler]
        private void Handle(string message)
        {
            MessageFilterAsync.LogBoard.Log($"Handle({message})");
        }

        [MessageHandler, Reentrant]
        private async Task Handle2(double message)
        {
            MessageFilterAsync.LogBoard.Log($"Handle2({message})");
            await Task.Yield();
            MessageFilterAsync.LogBoard.Log($"Handle2({message}) Done");
        }
    }

    public class MessageFilterAsync : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard;

        public MessageFilterAsync(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new FilterLogBoard();
        }

        [Fact]
        public async Task SyncHandler_With_AsyncFilter_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterAsyncActor>();
            actor.Tell("A");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            Assert.Equal(
                new[]
                {
                    "MessageFilterAsyncActor Async.OnPreMessageAsync",
                    "MessageFilterAsyncActor Async.OnPreMessageAsync Done",
                    "Handle(A)",
                    "MessageFilterAsyncActor Async.OnPostMessageAsync",
                    "MessageFilterAsyncActor Async.OnPostMessageAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public async Task AsyncHandler_With_AsyncFilter_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterAsyncActor>();
            actor.Tell(1.2);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            Assert.Equal(
                new[]
                {
                    "MessageFilterAsyncActor Async.OnPreMessageAsync",
                    "MessageFilterAsyncActor Async.OnPreMessageAsync Done",
                    "Handle2(1.2)",
                    "Handle2(1.2) Done",
                    "MessageFilterAsyncActor Async.OnPostMessageAsync",
                    "MessageFilterAsyncActor Async.OnPostMessageAsync Done"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
