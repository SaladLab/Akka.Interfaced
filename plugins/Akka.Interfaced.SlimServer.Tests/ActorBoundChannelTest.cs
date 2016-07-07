using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

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

            private Func<IActorContext, Tuple<IActorRef, TaggedType[], ActorBindingFlags>[]> _initialActorFactory;
            private Dictionary<int, IActorRef> _requestMap = new Dictionary<int, IActorRef>();
            private Dictionary<int, INotificationChannel> _observerChannelMap = new Dictionary<int, INotificationChannel>();

            public object Tag
            {
                get { return _tag; }
                set { _tag = value; }
            }

            public TestActorBoundChannel()
            {
            }

            public TestActorBoundChannel(Func<IActorContext, Tuple<IActorRef, TaggedType[], ActorBindingFlags>[]> initialActorFactory)
            {
                _initialActorFactory = initialActorFactory;
            }

            protected override void PreStart()
            {
                base.PreStart();

                if (_initialActorFactory != null)
                {
                    var actors = _initialActorFactory(Context);
                    if (actors != null)
                    {
                        foreach (var actor in actors)
                            BindActor(actor.Item1, actor.Item2.Select(t => new BoundType(t)), actor.Item3);
                    }
                }
            }

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

                var observerUpdatable = message.InvokePayload as IPayloadObserverUpdatable;
                if (observerUpdatable != null)
                {
                    observerUpdatable.Update(o =>
                    {
                        var observer = ((InterfacedObserver)o);
                        if (observer != null)
                        {
                            // naive observer channel management. but this is good enough,
                            // because client-side of slim channel usually keep observer id unique.
                            _observerChannelMap[observer.ObserverId] = observer.Channel;

                            observer.Channel = new AkkaReceiverNotificationChannel(Self);
                        }
                    });
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
                _observerChannelMap[m.ObserverId].Notify(m);
            }

            protected override void OnCloseRequest()
            {
                Close();
            }
        }

        public class DummyActor : InterfacedActor, IDummyExFinalSync, IDummyWithTagSync
        {
            public DummyActor()
            {
            }

            object IDummySync.Call(object param)
            {
                return "Call:" + param;
            }

            object IDummyExSync.CallEx(object param)
            {
                return "CallEx:" + param;
            }

            object IDummyEx2Sync.CallEx2(object param)
            {
                return "CallEx2:" + param;
            }

            object IDummyExFinalSync.CallExFinal(object param)
            {
                return "CallExFinal:" + param;
            }

            object IDummyWithTagSync.CallWithTag(object param, string id)
            {
                return "CallWithTag:" + param + ":" + id;
            }
        }

        public class DummyEventActor : InterfacedActor, IDummySync, IDummyWithTagSync, IActorBoundChannelObserver
        {
            internal object _tagByChannelOpen;
            internal object _tagByChannelClose;

            object IDummySync.Call(object param)
            {
                return "Call:" + param;
            }

            object IDummyWithTagSync.CallWithTag(object param, string id)
            {
                return "CallWithTag:" + param + ":" + id;
            }

            void IActorBoundChannelObserver.ChannelOpen(IActorBoundChannel channel, object tag)
            {
                _tagByChannelOpen = tag;
            }

            void IActorBoundChannelObserver.ChannelOpenTimeout(object tag)
            {
            }

            void IActorBoundChannelObserver.ChannelClose(IActorBoundChannel channel, object tag)
            {
                _tagByChannelClose = tag;
                Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        public class SubjectActor : InterfacedActor, ISubjectSync
        {
            private List<ISubjectObserver> _observers = new List<ISubjectObserver>();

            void ISubjectSync.MakeEvent(string eventName)
            {
                foreach (var observer in _observers)
                    observer.Event(eventName);
            }

            void ISubjectSync.Subscribe(ISubjectObserver observer)
            {
                _observers.Add(observer);
            }

            void ISubjectSync.Unsubscribe(ISubjectObserver observer)
            {
                _observers.Remove(observer);
            }
        }

        [Fact]
        private async Task RequestToBoundActor_Response()
        {
            // Arrange
            var channel = ActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = ActorOf<DummyActor>();
            var boundActor = await channel.BindActor(dummy, new TaggedType[] { typeof(IDummyExFinal) });
            Assert.NotNull(boundActor);

            // Act
            var r2 = await channel.CastToIActorRef().Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = boundActor.Id,
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
            var channel = ActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = ActorOf<DummyActor>();
            var boundActor = await channel.BindActor(dummy, new[] { new TaggedType(typeof(IDummyWithTag), "ID") });
            Assert.NotNull(boundActor);

            // Act
            var r2 = await channel.CastToIActorRef().Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = boundActor.Id,
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
            var channel = ActorOf<TestActorBoundChannel>();
            var dummy = ActorOf<DummyActor>();

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
            var channel = ActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = ActorOf<DummyActor>();
            var boundActor = await channel.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotNull(boundActor);

            // Act
            var r2 = await channel.CastToIActorRef().Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = boundActor.Id,
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
            var channel = ActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = ActorOf<DummyActor>();
            var boundActor = await channel.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotNull(boundActor);

            // Act
            var done = await channel.UnbindActor(dummy);
            Assert.True(done);
            var r2 = await channel.CastToIActorRef().Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = boundActor.Id,
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
            var channel = ActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = ActorOf<DummyActor>();
            var boundActor = await channel.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotNull(boundActor);

            // Act
            var done = await channel.BindType(dummy, new TaggedType[] { typeof(IDummyEx2) });
            Assert.True(done);
            var r2 = await channel.CastToIActorRef().Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = boundActor.Id,
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
            var channel = ActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = ActorOf<DummyActor>();
            var boundActor = await channel.BindActor(dummy, new TaggedType[] { typeof(IDummyEx) });
            Assert.NotNull(boundActor);

            // Act
            var done = await channel.BindType(dummy, new TaggedType[] { typeof(IDummyEx2) });
            Assert.True(done);
            var done2 = await channel.UnbindType(dummy, new[] { typeof(IDummyEx2) });
            Assert.True(done2);
            var r2 = await channel.CastToIActorRef().Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = boundActor.Id,
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
        private async Task Notification_RedirectedViaChannel()
        {
            // Arrange
            var channel = ActorOf(Props.Create(() => new TestActorBoundChannel(context =>
                new[] { Tuple.Create(context.ActorOf<SubjectActor>(null), new TaggedType[] { typeof(ISubject) }, ActorBindingFlags.CloseThenStop) })));
            var channelRef = channel.Cast<ActorBoundChannelRef>();
            var actorId = 1;

            // Act
            var notifications = new List<NotificationMessage>();
            var r1 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
                Message = new RequestMessage
                {
                    RequestId = 1,
                    InvokePayload = new ISubject_PayloadTable.Subscribe_Invoke
                    {
                        observer = new SubjectObserver(new TestNotificationChannel { Messages = notifications }, 1)
                    }
                }
            });
            Assert.Equal(1, r1.RequestId);
            var r2 = await channel.Ask<ResponseMessage>(new TestActorBoundChannel.Request
            {
                ActorId = actorId,
                Message = new RequestMessage
                {
                    RequestId = 2,
                    InvokePayload = new ISubject_PayloadTable.MakeEvent_Invoke
                    {
                        eventName = "Test"
                    }
                }
            });
            Assert.Equal(2, r2.RequestId);

            // Assert
            Assert.Equal(1, notifications.Count);
            Assert.Equal(1, notifications[0].ObserverId);
            Assert.Equal("Test", ((ISubjectObserver_PayloadTable.Event_Invoke)notifications[0].InvokePayload).eventName);
        }

        [Fact]
        private async Task OpenChannel_SendOpenChannelNotification()
        {
            // Arrange
            var channelActor = ActorOfAsTestActorRef<TestActorBoundChannel>();
            channelActor.UnderlyingActor.Tag = "Tag";
            var channel = channelActor.Cast<ActorBoundChannelRef>();
            var dummy = ActorOfAsTestActorRef<DummyEventActor>();
            var dummyActor = dummy.UnderlyingActor;
            var boundActor = await channel.BindActor(dummy, new[] { new TaggedType(typeof(IDummyWithTag), "ID") }, ActorBindingFlags.OpenThenNotification);
            Assert.NotNull(boundActor);

            // Act
            channel.WithNoReply().Close();
            Watch(channel.CastToIActorRef());
            ExpectTerminated(channel.CastToIActorRef());

            // Assert
            Assert.Equal("Tag", dummyActor._tagByChannelOpen);
        }

        [Fact]
        private async Task CloseChannel_SendClosedChannelNotification()
        {
            // Arrange
            var channelActor = ActorOfAsTestActorRef<TestActorBoundChannel>();
            channelActor.UnderlyingActor.Tag = "Tag";
            var channel = channelActor.Cast<ActorBoundChannelRef>();
            var dummy = ActorOfAsTestActorRef<DummyEventActor>();
            var dummyActor = dummy.UnderlyingActor;
            var boundActor = await channel.BindActor(dummy, new[] { new TaggedType(typeof(IDummyWithTag), "ID") }, ActorBindingFlags.CloseThenNotification);
            Assert.NotNull(boundActor);

            // Act
            channel.WithNoReply().Close();
            Watch(channel.CastToIActorRef());
            ExpectTerminated(channel.CastToIActorRef());

            // Assert
            Assert.Equal("Tag", dummyActor._tagByChannelClose);
        }

        [Fact]
        private async Task CloseChannel_SendInterfacedPoisonPill()
        {
            // Arrange
            var channelActor = ActorOfAsTestActorRef<TestActorBoundChannel>();
            channelActor.UnderlyingActor.Tag = "Tag";
            var channel = channelActor.Cast<ActorBoundChannelRef>();
            var dummy = ActorOfAsTestActorRef<DummyEventActor>();
            var dummyActor = dummy.UnderlyingActor;
            var boundActor = await channel.BindActor(dummy, new[] { new TaggedType(typeof(IDummyWithTag), "ID") }, ActorBindingFlags.CloseThenStop);
            Assert.NotNull(boundActor);

            // Act
            channel.WithNoReply().Close();
            Watch(channel.CastToIActorRef());
            ExpectTerminated(channel.CastToIActorRef());

            // Assert
            Assert.Null(dummyActor._tagByChannelClose);
        }

        [Fact]
        private void CloseChannel_WithChildren_WaitsForAllChildrenStop()
        {
            // Arrange
            var channel = Sys.InterfacedActorOf(() => new TestActorBoundChannel(context =>
                new[] { Tuple.Create(context.ActorOf<DummyEventActor>(null), new TaggedType[] { typeof(IDummy) }, ActorBindingFlags.CloseThenNotification) })).Cast<ActorBoundChannelRef>();

            // Act
            channel.WithNoReply().Close();
            Watch(channel.CastToIActorRef());
            ExpectTerminated(channel.CastToIActorRef());
        }

        [Fact]
        private async Task ChildActorStops_Then_CloseChannel()
        {
            // Arrange
            var channel = Sys.InterfacedActorOf<TestActorBoundChannel>().Cast<ActorBoundChannelRef>();
            var dummy = Sys.InterfacedActorOf<DummyActor>().Cast<DummyRef>();
            var boundActor = (DummyRef)(await channel.BindActor(dummy, ActorBindingFlags.StopThenCloseChannel));
            Assert.NotNull(boundActor);

            // Act
            dummy.CastToIActorRef().Tell(InterfacedPoisonPill.Instance);
            Watch(channel.CastToIActorRef());
            ExpectTerminated(channel.CastToIActorRef());
        }
    }
}
