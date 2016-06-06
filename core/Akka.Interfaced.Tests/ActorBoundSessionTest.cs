using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
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

            var boundType = GetBoundType(boundActor, message.InvokePayload);
            if (boundType == null)
            {
                Sender.Tell(new ResponseMessage { RequestId = message.RequestId, Exception = new RequestMessageException() });
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

        private BoundType GetBoundType(BoundActor boundActor, IInterfacedPayload payload)
        {
            var interfaceType = payload.GetInterfaceType();

            foreach (var t in boundActor.DerivedTypes)
            {
                if (t.Type == interfaceType)
                    return t;
            }

            return null;
        }
    }

    public class DummyWorkerActor : InterfacedActor, IDummyExFinal, IWorker
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

        Task IWorker.Atomic(int id)
        {
            throw new NotImplementedException();
        }

        Task IWorker.Reentrant(int id)
        {
            throw new NotImplementedException();
        }
    }

    public class ActorBoundSessionTest : Akka.TestKit.Xunit2.TestKit
    {
        public ActorBoundSessionTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        private async Task Test_Work()
        {
            var session = ActorOfAsTestActorRef<TestActorBoundSesson>();
            var dummy = ActorOfAsTestActorRef<DummyWorkerActor>();
            var r = await session.Ask<ActorBoundSessionMessage.BindReply>(new ActorBoundSessionMessage.Bind(dummy, typeof(IDummyExFinal)));
            Assert.NotEqual(0, r.ActorId);

            var r2 = await session.Ask<ResponseMessage>(new TestActorBoundSesson.Request
            {
                ActorId = 1,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new IDummy_PayloadTable.Call_Invoke { param = "Test" }
                }
            });
            Assert.Equal(1, r2.RequestId);
            Assert.Equal("Call:Test", r2.ReturnPayload.Value);
        }
    }
}
