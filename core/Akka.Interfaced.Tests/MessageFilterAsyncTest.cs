using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class MessageFilterAsyncTest : TestKit.Xunit2.TestKit
    {
        public MessageFilterAsyncTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class MessageFilterAsyncAttribute
            : Attribute, IFilterPerClassFactory, IPreMessageAsyncFilter, IPostMessageAsyncFilter
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => this;

            int IFilter.Order => 0;

            async Task IPreMessageAsyncFilter.OnPreMessageAsync(PreMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPreMessageAsync");
                await Task.Yield();
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPreMessageAsync Done");
            }

            async Task IPostMessageAsyncFilter.OnPostMessageAsync(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPostMessageAsync");
                await Task.Yield();
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPostMessageAsync Done");
            }
        }

        [MessageFilterAsync]
        public class MessageFilterAsyncActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public MessageFilterAsyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }

            [MessageHandler, Reentrant]
            private async Task Handle2(double message)
            {
                _log.Add($"Handle2({message})");
                await Task.Yield();
                _log.Add($"Handle2({message}) Done");
            }
        }

        [Fact]
        public async Task SyncHandler_With_AsyncFilter_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterAsyncActor(log));

            // Act
            actor.Tell("A");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(
                new[]
                {
                    "MessageFilterAsyncActor Async.OnPreMessageAsync",
                    "MessageFilterAsyncActor Async.OnPreMessageAsync Done",
                    "Handle(A)",
                    "MessageFilterAsyncActor Async.OnPostMessageAsync",
                    "MessageFilterAsyncActor Async.OnPostMessageAsync Done"
                },
                log);
        }

        [Fact]
        public async Task AsyncHandler_With_AsyncFilter_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterAsyncActor(log));

            // Act
            actor.Tell(1.2);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
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
                log);
        }
    }
}
