using System;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class RequestFilterOrderTest : TestKit.Xunit2.TestKit
    {
        public RequestFilterOrderTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        // FilterFirst

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterFirstAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new RequestFilterFirstFilter(_actorType.Name + "_1");
        }

        public class RequestFilterFirstFilter : IPreRequestFilter, IPostRequestFilter
        {
            private readonly string _name;

            public RequestFilterFirstFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 1;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        // FilterSecond

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterSecondAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new RequestFilterSecondFilter(_actorType.Name + "_2");
        }

        public class RequestFilterSecondFilter : IPreRequestFilter, IPostRequestFilter
        {
            private readonly string _name;

            public RequestFilterSecondFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 2;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        public class TestFilterActor : InterfacedActor, IDummySync
        {
            private LogBoard<string> _log;

            public TestFilterActor(LogBoard<string> log)
            {
                _log = log;
            }

            [RequestFilterFirst, RequestFilterSecond]
            object IDummySync.Call(object param)
            {
                _log.Add($"Call({param})");
                return param;
            }
        }

        [Fact]
        public async Task FilterOrder_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(() => new TestFilterActor(log)).Cast<DummyRef>();

            // Act
            await a.Call("A");

            // Assert
            Assert.Equal(
                new[]
                {
                    "TestFilterActor_1.OnPreRequest",
                    "TestFilterActor_2.OnPreRequest",
                    "Call(A)",
                    "TestFilterActor_2.OnPostRequest",
                    "TestFilterActor_1.OnPostRequest"
                },
                log);
        }
    }
}
