using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    // FilterFirst

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterFirstAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new TestFilterFirstFilter(_actorType.Name + "_1");
        }
    }

    public class TestFilterFirstFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _name;

        public TestFilterFirstFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 1;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterOrder.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterOrder.LogBoard.Log($"{_name}.OnPostHandle");
        }
    }

    // FilterSecond

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestFilterSecondAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new TestFilterSecondFilter(_actorType.Name + "_2");
        }
    }

    public class TestFilterSecondFilter : IPreRequestFilter, IPostRequestFilter
    {
        private readonly string _name;

        public TestFilterSecondFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 2;

        void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
        {
            TestFilterOrder.LogBoard.Log($"{_name}.OnPreHandle");
        }

        void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
        {
            TestFilterOrder.LogBoard.Log($"{_name}.OnPostHandle");
        }
    }

    public class TestFilterOrderActor : InterfacedActor<TestFilterOrderActor>, IDummy
    {
        [TestFilterFirst, TestFilterSecond]
        Task<object> IDummy.Call(object param)
        {
            return Task.FromResult(param);
        }
    }

    public class TestFilterOrder : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard = new FilterLogBoard();

        public TestFilterOrder(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_FilterOrder_Work()
        {
            var actor = ActorOfAsTestActorRef<TestFilterOrderActor>();
            var a = new DummyRef(actor);
            await a.Call(null);

            Assert.Equal(
                new List<string>
                {
                    "TestFilterOrderActor_1.OnPreHandle",
                    "TestFilterOrderActor_2.OnPreHandle",
                    "TestFilterOrderActor_2.OnPostHandle",
                    "TestFilterOrderActor_1.OnPostHandle"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
