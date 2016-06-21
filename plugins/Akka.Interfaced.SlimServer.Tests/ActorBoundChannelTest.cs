using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced.SlimServer
{
    public class ActorBoundChannelTest : TestKit.Xunit2.TestKit
    {
        public ActorBoundChannelTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestActorBoundChannel : ActorBoundChannelBase
        {
            public class Request
            {
                public int ActorId;
                public RequestMessage Message;
            }

            private Dictionary<int, IActorRef> _requestMap = new Dictionary<int, IActorRef>();

            [MessageHandler]
            private void Handle(Request m)
            {
                var message = m.Message;

                var boundActor = GetBoundActor(m.ActorId);
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
                boundActor.Actor.Tell(message, Self);
            }

            protected override void OnResponseMessage(ResponseMessage m)
            {
                _requestMap[m.RequestId].Tell(m);
                _requestMap.Remove(m.RequestId);
            }

            protected override void OnNotificationMessage(NotificationMessage m)
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
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var channelRef = new ActorBoundChannelRef(channel);
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var actorId = await channelRef.BindActor(dummy, new TaggedType[] { typeof(IDummyExFinal) });
            Assert.NotEqual(0, actorId);

            // Act
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
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
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var channelRef = new ActorBoundChannelRef(channel);
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var actorId = await channelRef.BindActor(dummy, new[] { new TaggedType(typeof(IDummyWithTag), "ID") });
            Assert.NotEqual(0, actorId);

            // Act
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
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
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();

            // Act
            var r = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
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
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var channelRef = new ActorBoundChannelRef(channel);
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var actorId = await channelRef.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotEqual(0, actorId);

            // Act
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
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
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var channelRef = new ActorBoundChannelRef(channel);
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var actorId = await channelRef.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotEqual(0, actorId);

            // Act
            var done = await channelRef.UnbindActor(dummy);
            Assert.True(done);
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
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
        private async Task BindTypeToBoundActor_And_RequestToBoundActor_Response()
        {
            // Arrange
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var channelRef = new ActorBoundChannelRef(channel);
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var actorId = await channelRef.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotEqual(0, actorId);

            // Act
            var done = await channelRef.BindType(dummy, new TaggedType[] { typeof(IDummyEx2) });
            Assert.True(done);
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
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
        private async Task UnbindTypeToBoundActor_And_RequestOnUnboundType_Exception()
        {
            // Arrange
            var channel = ActorOfAsTestActorRef<TestActorBoundChannel>();
            var channelRef = new ActorBoundChannelRef(channel);
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var actorId = await channelRef.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotEqual(0, actorId);

            // Act
            var done = await channelRef.BindType(dummy, new TaggedType[] { typeof(IDummyEx2) });
            Assert.True(done);
            var done2 = await channelRef.UnbindType(dummy, new[] { typeof(IDummyEx2) });
            Assert.True(done2);
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
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
