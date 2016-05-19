using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    // FilterFirst

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestFilterFirstAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new RequestFilterFirstFilter(_actorType.Name + "_1");
        }
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
            RequestFilterOrder.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterOrder.LogBoard.Log($"{_name}.OnPostRequest");
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

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new RequestFilterSecondFilter(_actorType.Name + "_2");
        }
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
            RequestFilterOrder.LogBoard.Log($"{_name}.OnPreRequest");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            RequestFilterOrder.LogBoard.Log($"{_name}.OnPostRequest");
        }
    }

    public class RequestFilterOrderActor : InterfacedActor, IDummy
    {
        [RequestFilterFirst, RequestFilterSecond]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }
    }

    public class RequestFilterOrder : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public RequestFilterOrder(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task FilterOrder_Work()
        {
            var actor = ActorOfAsTestActorRef<RequestFilterOrderActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new[]
                {
                    "RequestFilterOrderActor_1.OnPreRequest",
                    "RequestFilterOrderActor_2.OnPreRequest",
                    "RequestFilterOrderActor_2.OnPostRequest",
                    "RequestFilterOrderActor_1.OnPostRequest"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
