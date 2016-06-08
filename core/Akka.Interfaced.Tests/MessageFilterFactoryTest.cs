using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class MessageFilterFactoryTest : TestKit.Xunit2.TestKit
    {
        public MessageFilterFactoryTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        // FilterPerClass

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class MessageFilterPerClassAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new MessageFilterPerClassFilter(_actorType.Name);
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
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        public class MessageFilterPerClassActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public MessageFilterPerClassActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler, MessageFilterPerClass]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }
        }

        [Fact]
        public async Task FilterPerClass_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterPerClassActor(log));

            // Act
            actor.Tell("A");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(
                new[]
                {
                    "MessageFilterPerClassActor.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerClassActor.OnPostMessage"
                },
                log);
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
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        public class MessageFilterPerClassMethodActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public MessageFilterPerClassMethodActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler, MessageFilterPerClassMethod]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }
        }

        [Fact]
        public async Task FilterPerClassMethod_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterPerClassMethodActor(log));

            // Act
            actor.Tell("A");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            Assert.Equal(
                new[]
                {
                    "MessageFilterPerClassMethodActor.Handle.OnPreMessage",
                    "Handle(A)",
                    "MessageFilterPerClassMethodActor.Handle.OnPostMessage"
                },
                log);
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
                return new MessageFilterPerInstanceFilter(actor != null ? _actorType.Name : null, actor);
            }
        }

        public class MessageFilterPerInstanceFilter : IPreMessageFilter, IPostMessageFilter
        {
            private string _name;

            public MessageFilterPerInstanceFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        [MessageFilterPerInstance]
        public class MessageFilterPerInstanceActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public MessageFilterPerInstanceActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }

            [MessageHandler]
            private void Handle2(double message)
            {
                _log.Add($"Handle2({message})");
            }
        }

        [Fact]
        public async Task FilterPerInstance_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterPerInstanceActor(log));

            // Act
            actor.Tell("A");
            actor.Tell(1.2);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
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
                log);
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
                return new MessageFilterPerInstanceMethodFilter(actor != null ? name : null, actor);
            }
        }

        public class MessageFilterPerInstanceMethodFilter : IPreMessageFilter, IPostMessageFilter
        {
            private string _name;

            public MessageFilterPerInstanceMethodFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        [MessageFilterPerInstanceMethod]
        public class MessageFilterPerInstanceMethodActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public MessageFilterPerInstanceMethodActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }

            [MessageHandler]
            private void Handle2(double message)
            {
                _log.Add($"Handle2({message})");
            }
        }

        [Fact]
        public async Task FilterPerInstanceMethod_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterPerInstanceMethodActor(log));

            // Act
            actor.Tell("A");
            actor.Tell(1.2);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
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
                log);
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
                return new MessageFilterPerMessageFilter(actor != null ? name : null, actor);
            }
        }

        public class MessageFilterPerMessageFilter : IPreMessageFilter, IPostMessageFilter
        {
            private string _name;

            public MessageFilterPerMessageFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreMessageFilter.OnPreMessage(PreMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreMessage");
            }

            void IPostMessageFilter.OnPostMessage(PostMessageFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostMessage");
            }
        }

        [MessageFilterPerMessage]
        public class MessageFilterPerMessageActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public MessageFilterPerMessageActor(LogBoard<string> log)
            {
                _log = log;
            }

            [MessageHandler]
            private void Handle(string message)
            {
                _log.Add($"Handle({message})");
            }
        }

        [Fact]
        public async Task FilterPerMessage_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var actor = ActorOf(() => new MessageFilterPerMessageActor(log));

            // Act
            actor.Tell("A");
            actor.Tell("B");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
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
                log);
        }
    }
}
