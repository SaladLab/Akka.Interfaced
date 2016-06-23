using System;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class RequestFilterAsyncTest : TestKit.Xunit2.TestKit
    {
        public RequestFilterAsyncTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterAsyncAttribute
             : Attribute, IFilterPerClassFactory, IPreRequestAsyncFilter, IPostRequestAsyncFilter
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => this;

            int IFilter.Order => 0;

            async Task IPreRequestAsyncFilter.OnPreRequestAsync(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPreRequestAsync");
                await Task.Yield();
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPreRequestAsync Done");
            }

            async Task IPostRequestAsyncFilter.OnPostRequestAsync(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPostRequestAsync");
                await Task.Yield();
                LogBoard<string>.Add(context.Actor, $"{_actorType.Name} Async.OnPostRequestAsync Done");
            }
        }

        [RequestFilterAsync]
        public class RequestFilterAsyncActor : InterfacedActor, IExtendedInterface<IWorker>
        {
            private LogBoard<string> _log;

            public RequestFilterAsyncActor(LogBoard<string> log)
            {
                _log = log;
            }

            [ExtendedHandler]
            private void Atomic(int id)
            {
                _log.Add($"Atomic {id}");
            }

            [ExtendedHandler, Reentrant]
            private async Task Reentrant(int id)
            {
                _log.Add($"Reentrant {id}");
                await Task.Yield();
                _log.Add($"Reentrant Done {id}");
            }
        }

        [Fact]
        public async Task SyncHandler_With_AsyncFilter_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(() => new RequestFilterAsyncActor(log)).Cast<WorkerRef>();

            // Act
            await a.Atomic(1);

            // Assert
            Assert.Equal(
                new[]
                {
                    "RequestFilterAsyncActor Async.OnPreRequestAsync",
                    "RequestFilterAsyncActor Async.OnPreRequestAsync Done",
                    "Atomic 1",
                    "RequestFilterAsyncActor Async.OnPostRequestAsync",
                    "RequestFilterAsyncActor Async.OnPostRequestAsync Done"
                },
                log);
        }

        [Fact]
        public async Task AsyncHandler_With_AsyncFilter_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(() => new RequestFilterAsyncActor(log)).Cast<WorkerRef>();

            // Act
            await a.Reentrant(1);

            // Assert
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
                log);
        }
    }
}
