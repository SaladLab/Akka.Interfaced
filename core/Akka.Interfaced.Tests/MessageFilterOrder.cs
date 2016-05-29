using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    // FilterFirst

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterFirstAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new MessageFilterFirstFilter(_actorType.Name + "_1");
        }
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
            MessageFilterOrder.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterOrder.LogBoard.Log($"{_name}.OnPostMessage");
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

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new MessageFilterSecondFilter(_actorType.Name + "_2");
        }
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
            MessageFilterOrder.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterOrder.LogBoard.Log($"{_name}.OnPostMessage");
        }
    }

    public class MessageFilterOrderActor : InterfacedActor
    {
        [MessageHandler, MessageFilterFirst, MessageFilterSecond]
        private void Handle(string message)
        {
            MessageFilterOrder.LogBoard.Log($"Handle({message})");
        }
    }

    public class MessageFilterOrder : Akka.TestKit.Xunit2.TestKit
    {
        public static LogBoard LogBoard;

        public MessageFilterOrder(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new LogBoard();
        }

        [Fact]
        public void FilterOrder_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterOrderActor>();
            actor.Tell("A");

            Assert.Equal(
                new[]
                {
                    "MessageFilterOrderActor_1.OnPreMessage",
                    "MessageFilterOrderActor_2.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterOrderActor_2.OnPostMessage",
                    "MessageFilterOrderActor_1.OnPostMessage"
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
