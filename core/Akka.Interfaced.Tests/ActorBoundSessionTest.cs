using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class ActorBoundSessionTest : TestKit.Xunit2.TestKit
    {
        public ActorBoundSessionTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestActorBoundSesson : ActorBoundSession
        {
            public class Request
            {
                public int ActorId;
                public RequestMessage Message;
            }

            private Dictionary<int, IActorRef> _requestMap = new Dictionary<int, IActorRef>();

            protected override void OnReceive(object message)
            {
                var requestMessage = message as Request;
                if (requestMessage != null)
                {
                    OnRequestMessage(requestMessage.ActorId, requestMessage.Message);
                    return;
                }

                base.OnReceive(message);
            }

            protected void OnRequestMessage(int actorId, RequestMessage message)
            {
                var boundActor = GetBoundActor(actorId);
                if (boundActor == null)
                {
                    Sender.Tell(new ResponseMessage { RequestId = message.RequestId, Exception = new RequestTargetException() });
                    return;
                }

                var boundType = boundActor.FindBoundType(message.InvokePayload.GetInterfaceType());
                if (boundType == null)
                {
                    Sender.Tell(new ResponseMessage { RequestId = message.RequestId, Exception = new RequestHandlerNotFoundException() });
                    return;
                }

                if (boundType.IsTagOverridable)
                {
                    var msg = (IPayloadTagOverridable)message.InvokePayload;
                    msg.SetTag(boundType.TagValue);
                }

                _requestMap[message.RequestId] = Sender;
                boundActor.Actor.Tell(message);
            }

            protected override void OnResponseMessage(ResponseMessage message)
            {
                _requestMap[message.RequestId].Tell(message);
                _requestMap.Remove(message.RequestId);
            }

            protected override void OnNotificationMessage(NotificationMessage message)
            {
            }
        }

        public class DummyWorkerActor : InterfacedActor, IDummyExFinal, IDummyWithTag
        {
            public DummyWorkerActor()
            {
            }

            Task<object> IDummy.Call(object param)
            {
                return Task.FromResult<object>("Call:" + param);
            }

            Task<object> IDummyEx.CallEx(object param)
            {
                return Task.FromResult<object>("CallEx:" + param);
            }

            Task<object> IDummyEx2.CallEx2(object param)
            {
                return Task.FromResult<object>("CallEx2:" + param);
            }

            Task<object> IDummyExFinal.CallExFinal(object param)
            {
                return Task.FromResult<object>("CallExFinal:" + param);
            }

            Task<object> IDummyWithTag.CallWithTag(object param, string id)
            {
                return Task.FromResult<object>("CallWithTag:" + param + ":" + id);
            }
        }

        [Fact]
        private async Task RequestToBoundActor_Response()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyExFinal)));
            Assert.NotEqual(0, r.ActorId);

            // Act
            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = r.ActorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummy_PayloadTable.Call_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r2.RequestId);
            Assert.Equal("Call:Test", r2.ReturnPayload.Value);
        }

        [Fact]
        private async Task RequestToBoundActorWithTag_Response()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyWithTag), "ID"));
            Assert.NotEqual(0, r.ActorId);

            // Act
            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = r.ActorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummyWithTag_PayloadTable.CallWithTag_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r2.RequestId);
            Assert.Equal("CallWithTag:Test:ID", r2.ReturnPayload.Value);
        }

        [Fact]
        private async Task RequestToUnboundActor_Exception()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();

            // Act
            var r = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = 1,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummy_PayloadTable.Call_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r.RequestId);
            Assert.IsType<RequestTargetException>(r.Exception);
        }

        [Fact]
        private async Task RequestOnUnboundType_Exception()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyEx)));
            Assert.NotEqual(0, r.ActorId);

            // Act
            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = r.ActorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummyEx2_PayloadTable.CallEx2_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r2.RequestId);
            Assert.IsType<RequestHandlerNotFoundException>(r2.Exception);
        }

        [Fact]
        private async Task UnbindActor_And_RequestToUnboundActor_Exception()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyEx)));
            Assert.NotEqual(0, r.ActorId);

            // Act
            session.Tell(new ActorBoundSessionMessage.Unbind(dummy));
            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = r.ActorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummyEx2_PayloadTable.CallEx2_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r2.RequestId);
            Assert.IsType<RequestTargetException>(r2.Exception);
        }

        [Fact]
        private async Task AddTypeToBoundActor_And_RequestToBoundActor_Response()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyEx)));
            Assert.NotEqual(0, r.ActorId);

            // Act
            session.Tell(new ActorBoundSessionMessage.AddType(dummy, typeof(IDummyEx2)));
            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = r.ActorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummyEx2_PayloadTable.CallEx2_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r2.RequestId);
            Assert.Equal("CallEx2:Test", r2.ReturnPayload.Value);
        }

        [Fact]
        private async Task RemoveTypeToBoundActor_And_RequestOnUnboundType_Exception()
        {
            // Arrange
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyEx)));
            Assert.NotEqual(0, r.ActorId);

            // Act
            session.Tell(new ActorBoundSessionMessage.AddType(dummy, typeof(IDummyEx2)));
            session.Tell(new ActorBoundSessionMessage.RemoveType(dummy, typeof(IDummyEx2)));
            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = r.ActorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummyEx2_PayloadTable.CallEx2_Invoke { param = "Test" }
                }
            });

            // Assert
            Assert.Equal(1, r2.RequestId);
            Assert.IsType<RequestHandlerNotFoundException>(r2.Exception);
        }
    }
}
