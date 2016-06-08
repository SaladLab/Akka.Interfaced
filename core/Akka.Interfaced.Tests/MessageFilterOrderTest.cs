using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class MessageFilterOrderTest : TestKit.Xunit2.TestKit
    {
        public MessageFilterOrderTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        // FilterFirst

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class MessageFilterFirstAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new MessageFilterFirstFilter(_actorType.Name + "_1");
        }

        public class MessageFilterFirstFilter : IPreMessageFilter, IPostMessageFilter
        {
            private readonly string _name;

            public MessageFilterFirstFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 1;

            void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        // FilterSecond

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class MessageFilterSecondAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new MessageFilterSecondFilter(_actorType.Name + "_2");
        }

        public class MessageFilterSecondFilter : IPreMessageFilter, IPostMessageFilter
        {
            private readonly string _name;

            public MessageFilterSecondFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 2;

            void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        public class TestFilterActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestFilterActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler, MessageFilterFirst, MessageFilterSecond]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }
        }

        [Fact]
        public async Task FilterOrder_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new TestFilterActor(log));

            // Act
            actor.Tell("A");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(
                new[]
                {
                    "TestFilterActor_1.OnPreMessage",
                    "TestFilterActor_2.OnPreMessage",
                    "Handle(A)",
                    "TestFilterActor_2.OnPostMessage",
                    "TestFilterActor_1.OnPostMessage"
                },
                log);
        }
    }
}
