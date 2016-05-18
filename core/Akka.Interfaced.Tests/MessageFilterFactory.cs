using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    // FilterPerClass

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterPerClassAttribute : Attribute, IFilterPerClassFactory
    {
        private Type _actorType;

        void IFilterPerClassFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerClassFactory.CreateInstance()
        {
            return new MessageFilterPerClassFilter(_actorType.Name);
        }
    }

    public class MessageFilterPerClassFilter : IPreMessageFilter, IPostMessageFilter
    {
        private readonly string _name;

        public MessageFilterPerClassFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPostMessage");
        }
    }

    public class MessageFilterPerClassActor : InterfacedActor<MessageFilterPerClassActor>
    {
        [MessageHandler, MessageFilterPerClass]
        private void Handle(string message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle({message})");
        }
    }

    // FilterPerClassMethod

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterPerClassMethodAttribute : Attribute, IFilterPerClassMethodFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerClassMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerClassMethodFactory.CreateInstance()
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new MessageFilterPerClassMethodFilter(name);
        }
    }

    public class MessageFilterPerClassMethodFilter : IPreMessageFilter, IPostMessageFilter
    {
        private readonly string _name;

        public MessageFilterPerClassMethodFilter(string name)
        {
            _name = name;
        }

        int IFilter.Order => 0;

        void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPostMessage");
        }
    }

    public class MessageFilterPerClassMethodActor : InterfacedActor<MessageFilterPerClassMethodActor>
    {
        [MessageHandler, MessageFilterPerClassMethod]
        private void Handle(string message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle({message})");
        }
    }

    // FilterPerInstance

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterPerInstanceAttribute : Attribute, IFilterPerInstanceFactory
    {
        private Type _actorType;

        void IFilterPerInstanceFactory.Setup(Type actorType)
        {
            _actorType = actorType;
        }

        IFilter IFilterPerInstanceFactory.CreateInstance(object actor)
        {
            return new MessageFilterPerInstanceFilter(actor != null ? _actorType.Name : null);
        }
    }

    public class MessageFilterPerInstanceFilter : IPreMessageFilter, IPostMessageFilter
    {
        private string _name;

        public MessageFilterPerInstanceFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                MessageFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPostMessage");
        }
    }

    [MessageFilterPerInstance]
    public class MessageFilterPerInstanceActor : InterfacedActor<MessageFilterPerInstanceActor>
    {
        [MessageHandler]
        private void Handle(string message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle({message})");
        }

        [MessageHandler]
        private void Handle2(double message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle2({message})");
        }
    }

    // FilterPerInstanceMethod

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterPerInstanceMethodAttribute : Attribute, IFilterPerInstanceMethodFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerInstanceMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerInstanceMethodFactory.CreateInstance(object actor)
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new MessageFilterPerInstanceMethodFilter(actor != null ? name : null);
        }
    }

    public class MessageFilterPerInstanceMethodFilter : IPreMessageFilter, IPostMessageFilter
    {
        private string _name;

        public MessageFilterPerInstanceMethodFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                MessageFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPostMessage");
        }
    }

    [MessageFilterPerInstanceMethod]
    public class MessageFilterPerInstanceMethodActor : InterfacedActor<MessageFilterPerInstanceMethodActor>
    {
        [MessageHandler]
        private void Handle(string message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle({message})");
        }

        [MessageHandler]
        private void Handle2(double message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle2({message})");
        }
    }

    // FilterPerMessage

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class MessageFilterPerMessageAttribute : Attribute, IFilterPerInvokeFactory
    {
        private Type _actorType;
        private MethodInfo _method;

        void IFilterPerInvokeFactory.Setup(Type actorType, MethodInfo method)
        {
            _actorType = actorType;
            _method = method;
        }

        IFilter IFilterPerInvokeFactory.CreateInstance(object actor, object message)
        {
            var name = _actorType.Name + "." + _method.Name.Split('.').Last();
            return new MessageFilterPerMessageFilter(actor != null ? name : null);
        }
    }

    public class MessageFilterPerMessageFilter : IPreMessageFilter, IPostMessageFilter
    {
        private string _name;

        public MessageFilterPerMessageFilter(string name)
        {
            if (name != null)
            {
                _name = name;
                MessageFilterFactory.LogBoard.Log($"{_name}.Constructor");
            }
        }

        int IFilter.Order => 0;

        void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPreMessage");
        }

        void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
        {
            MessageFilterFactory.LogBoard.Log($"{_name}.OnPostMessage");
        }
    }

    [MessageFilterPerMessage]
    public class MessageFilterPerMessageActor : InterfacedActor<MessageFilterPerMessageActor>
    {
        [MessageHandler]
        private void Handle(string message)
        {
            MessageFilterFactory.LogBoard.Log($"Handle({message})");
        }
    }

    public class MessageFilterFactory : Akka.TestKit.Xunit2.TestKit
    {
        public static FilterLogBoard LogBoard;

        public MessageFilterFactory(ITestOutputHelper output)
            : base(output: output)
        {
            LogBoard = new FilterLogBoard();
        }

        [Fact]
        public void FilterPerClass_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterPerClassActor>();
            actor.Tell("A");

            Assert.Equal(
                new[]
                {
                    "MessageFilterPerClassActor.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerClassActor.OnPostMessage"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public void FilterPerClassMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterPerClassMethodActor>();
            actor.Tell("A");

            Assert.Equal(
                new[]
                {
                    "MessageFilterPerClassMethodActor.Handle.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerClassMethodActor.Handle.OnPostMessage"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public void FilterPerInstance_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterPerInstanceActor>();
            actor.Tell("A");
            actor.Tell(1.2);

            Assert.Equal(
                new[]
                {
                    "MessageFilterPerInstanceActor.Constructor",
                    "MessageFilterPerInstanceActor.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerInstanceActor.OnPostMessage",
                    "MessageFilterPerInstanceActor.OnPreMessage",
                    "Handle2(1.2)",
                    "MessageFilterPerInstanceActor.OnPostMessage"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public void FilterPerInstanceMethod_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterPerInstanceMethodActor>();
            actor.Tell("A");
            actor.Tell(1.2);

            Assert.Equal(
                new[]
                {
                    "MessageFilterPerInstanceMethodActor.Handle.Constructor",
                    "MessageFilterPerInstanceMethodActor.Handle2.Constructor",
                    "MessageFilterPerInstanceMethodActor.Handle.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerInstanceMethodActor.Handle.OnPostMessage",
                    "MessageFilterPerInstanceMethodActor.Handle2.OnPreMessage",
                    "Handle2(1.2)",
                    "MessageFilterPerInstanceMethodActor.Handle2.OnPostMessage"
                },
                LogBoard.GetAndClearLogs());
        }

        [Fact]
        public void FilterPerMessage_Work()
        {
            var actor = ActorOfAsTestActorRef<MessageFilterPerMessageActor>();
            actor.Tell("A");
            actor.Tell("B");

            Assert.Equal(
                new[]
                {
                    "MessageFilterPerMessageActor.Handle.Constructor",
                    "MessageFilterPerMessageActor.Handle.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerMessageActor.Handle.OnPostMessage",
                    "MessageFilterPerMessageActor.Handle.Constructor",
                    "MessageFilterPerMessageActor.Handle.OnPreMessage",
                    "Handle(B)",
                    "MessageFilterPerMessageActor.Handle.OnPostMessage",
                },
                LogBoard.GetAndClearLogs());
        }
    }
}
