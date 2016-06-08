using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced
{
    public class RequestFilterFactoryTest : TestKit.Xunit2.TestKit
    {
        public RequestFilterFactoryTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        // FilterPerClass

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterPerClassAttribute : Attribute, IFilterPerClassFactory
        {
            private Type _actorType;

            void IFilterPerClassFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerClassFactory.CreateInstance() => new RequestFilterPerClassFilter(_actorType.Name);
        }

        public class RequestFilterPerClassFilter : IPreRequestFilter, IPostRequestFilter
        {
            private readonly string _name;

            public RequestFilterPerClassFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 0;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        public class RequestFilterPerClassActor : InterfacedActor, IDummySync
        {
            private LogBoard<string> _log;

            public RequestFilterPerClassActor(LogBoard<string> log)
            {
                _log = log;
            }

            [RequestFilterPerClass]
            object IDummySync.Call(object param)
            {
                _log.Add($"Call({param})");
                return param;
            }
        }

        [Fact]
        public async Task FilterPerClass_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new DummyRef(ActorOf(() => new RequestFilterPerClassActor(log)));

            // Act
            await a.Call("A");

            // Assert
            Assert.Equal(
                new[]
                {
                    "RequestFilterPerClassActor.OnPreRequest",
                    "Call(A)",
                    "RequestFilterPerClassActor.OnPostRequest"
                },
                log);
        }

        // FilterPerClassMethod

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterPerClassMethodAttribute : Attribute, IFilterPerClassMethodFactory
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
                return new RequestFilterPerClassMethodFilter(name);
            }
        }

        public class RequestFilterPerClassMethodFilter : IPreRequestFilter, IPostRequestFilter
        {
            private readonly string _name;

            public RequestFilterPerClassMethodFilter(string name)
            {
                _name = name;
            }

            int IFilter.Order => 0;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        public class RequestFilterPerClassMethodActor : InterfacedActor, IDummySync
        {
            private LogBoard<string> _log;

            public RequestFilterPerClassMethodActor(LogBoard<string> log)
            {
                _log = log;
            }

            [RequestFilterPerClassMethod]
            object IDummySync.Call(object param)
            {
                _log.Add($"Call({param})");
                return param;
            }
        }

        [Fact]
        public async Task FilterPerClassMethod_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new DummyRef(ActorOf(() => new RequestFilterPerClassMethodActor(log)));

            // Act
            await a.Call("A");

            // Arrange
            Assert.Equal(
                new[]
                {
                    "RequestFilterPerClassMethodActor.Call.OnPreRequest",
                    "Call(A)",
                    "RequestFilterPerClassMethodActor.Call.OnPostRequest"
                },
                log);
        }

        // FilterPerInstance

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterPerInstanceAttribute : Attribute, IFilterPerInstanceFactory
        {
            private Type _actorType;

            void IFilterPerInstanceFactory.Setup(Type actorType)
            {
                _actorType = actorType;
            }

            IFilter IFilterPerInstanceFactory.CreateInstance(object actor)
            {
                return new RequestFilterPerInstanceFilter(actor != null ? _actorType.Name : null, actor);
            }
        }

        public class RequestFilterPerInstanceFilter : IPreRequestFilter, IPostRequestFilter
        {
            private string _name;

            public RequestFilterPerInstanceFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        [RequestFilterPerInstance]
        public class RequestFilterPerInstanceActor : InterfacedActor, IDummyExSync
        {
            private LogBoard<string> _log;

            public RequestFilterPerInstanceActor(LogBoard<string> log)
            {
                _log = log;
            }

            object IDummySync.Call(object param)
            {
                _log.Add($"Call({param})");
                return param;
            }

            object IDummyExSync.CallEx(object param)
            {
                _log.Add($"CallEx({param})");
                return param;
            }
        }

        [Fact]
        public async Task FilterPerInstance_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new DummyExRef(ActorOf(() => new RequestFilterPerInstanceActor(log)));

            // Act
            await a.Call("A");
            await a.CallEx("A");

            // Assert
            Assert.Equal(
                new[]
                {
                    "RequestFilterPerInstanceActor.Constructor",
                    "RequestFilterPerInstanceActor.OnPreRequest",
                    "Call(A)",
                    "RequestFilterPerInstanceActor.OnPostRequest",
                    "RequestFilterPerInstanceActor.OnPreRequest",
                    "CallEx(A)",
                    "RequestFilterPerInstanceActor.OnPostRequest"
                },
                log);
        }

        // FilterPerInstanceMethod

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterPerInstanceMethodAttribute : Attribute, IFilterPerInstanceMethodFactory
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
                return new RequestFilterPerInstanceMethodFilter(actor != null ? name : null, actor);
            }
        }

        public class RequestFilterPerInstanceMethodFilter : IPreRequestFilter, IPostRequestFilter
        {
            private string _name;

            public RequestFilterPerInstanceMethodFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        [RequestFilterPerInstanceMethod]
        public class RequestFilterPerInstanceMethodActor : InterfacedActor, IDummyExSync
        {
            private LogBoard<string> _log;

            public RequestFilterPerInstanceMethodActor(LogBoard<string> log)
            {
                _log = log;
            }

            object IDummySync.Call(object param)
            {
                _log.Add($"Call({param})");
                return param;
            }

            object IDummyExSync.CallEx(object param)
            {
                _log.Add($"CallEx({param})");
                return param;
            }
        }

        [Fact]
        public async Task FilterPerInstanceMethod_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new DummyExRef(ActorOf(() => new RequestFilterPerInstanceMethodActor(log)));

            // Act
            await a.Call("A");
            await a.Call("B");
            await a.CallEx("C");

            // Assert
            Assert.Equal(
                new[]
                {
                    "RequestFilterPerInstanceMethodActor.Call.Constructor",
                    "RequestFilterPerInstanceMethodActor.CallEx.Constructor",
                },
                log.Take(2).OrderBy(x => x));
            Assert.Equal(
                new[]
                {
                    "RequestFilterPerInstanceMethodActor.Call.OnPreRequest",
                    "Call(A)",
                    "RequestFilterPerInstanceMethodActor.Call.OnPostRequest",
                    "RequestFilterPerInstanceMethodActor.Call.OnPreRequest",
                    "Call(B)",
                    "RequestFilterPerInstanceMethodActor.Call.OnPostRequest",
                    "RequestFilterPerInstanceMethodActor.CallEx.OnPreRequest",
                    "CallEx(C)",
                    "RequestFilterPerInstanceMethodActor.CallEx.OnPostRequest"
                },
                log.Skip(2));
        }

        // FilterPerRequest

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class RequestFilterPerRequestAttribute : Attribute, IFilterPerInvokeFactory
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
                return new RequestFilterPerRequestFilter(actor != null ? name : null, actor);
            }
        }

        public class RequestFilterPerRequestFilter : IPreRequestFilter, IPostRequestFilter
        {
            private string _name;

            public RequestFilterPerRequestFilter(string name, object actor)
            {
                if (name != null)
                {
                    _name = name;
                    LogBoard<string>.Add(actor, $"{_name}.Constructor");
                }
            }

            int IFilter.Order => 0;

            void IPreRequestFilter.OnPreRequest(PreRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPreRequest");
            }

            void IPostRequestFilter.OnPostRequest(PostRequestFilterContext context)
            {
                LogBoard<string>.Add(context.Actor, $"{_name}.OnPostRequest");
            }
        }

        [RequestFilterPerRequest]
        public class RequestFilterPerRequestActor : InterfacedActor, IDummyExSync
        {
            private LogBoard<string> _log;

            public RequestFilterPerRequestActor(LogBoard<string> log)
            {
                _log = log;
            }

            object IDummySync.Call(object param)
            {
                _log.Add($"Call({param})");
                return param;
            }

            object IDummyExSync.CallEx(object param)
            {
                _log.Add($"CallEx({param})");
                return param;
            }
        }

        [Fact]
        public async Task FilterPerRequest_Work()
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = new DummyExRef(ActorOf(() => new RequestFilterPerRequestActor(log)));

            // Act
            await a.Call("A");
            await a.Call("B");
            await a.CallEx("C");

            // Assert
            Assert.Equal(
                new[]
                {
                    "RequestFilterPerRequestActor.Call.Constructor",
                    "RequestFilterPerRequestActor.Call.OnPreRequest",
                    "Call(A)",
                    "RequestFilterPerRequestActor.Call.OnPostRequest",
                    "RequestFilterPerRequestActor.Call.Constructor",
                    "RequestFilterPerRequestActor.Call.OnPreRequest",
                    "Call(B)",
                    "RequestFilterPerRequestActor.Call.OnPostRequest",
                    "RequestFilterPerRequestActor.CallEx.Constructor",
                    "RequestFilterPerRequestActor.CallEx.OnPreRequest",
                    "CallEx(C)",
                    "RequestFilterPerRequestActor.CallEx.OnPostRequest"
                },
                log);
        }
    }
}
