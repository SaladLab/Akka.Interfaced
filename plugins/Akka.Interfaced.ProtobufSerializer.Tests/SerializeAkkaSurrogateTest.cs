using System;
using Akka.Actor;
using ProtoBuf;
using TypeAlias;
using Xunit;

#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    public class SerializeAkkaSurrogateTest
    {
        [ProtoContract, TypeAlias]
        public class PathReturnMessage : IValueGetable
        {
            [ProtoMember(1)] public ActorPath v;

            public object Value
            {
                get { return v; }
            }
        }

        [Fact]
        public void TestSurrogateForActorPath()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                RequestId = 12345678,
                ReturnPayload = new PathReturnMessage { v = ActorPath.Parse("akka://system@host:1234/user/protobuf") }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ReturnPayload.Value.ToString(), obj2.ReturnPayload.Value.ToString());
        }

        [ProtoContract, TypeAlias]
        public class AddressReturnMessage : IValueGetable
        {
            [ProtoMember(1)] public Address v;

            public object Value
            {
                get { return v; }
            }
        }

        [Fact]
        public void TestSurrogateForAddress()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                RequestId = 12345678,
                ReturnPayload = new AddressReturnMessage { v = Address.Parse("akka://system@host:1234") }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ReturnPayload.Value.ToString(), obj2.ReturnPayload.Value.ToString());
        }

        [ProtoContract, TypeAlias]
        public class ActorReturnMessage : IValueGetable
        {
            [ProtoMember(1)] public IActorRef v;

            public object Value
            {
                get { return v; }
            }
        }

        public class DummyActor : UntypedActor
        {
            protected override void OnReceive(object message)
            {
            }
        }

        [Fact]
        public void TestSurrogateForIActorRef()
        {
            var system = ActorSystem.Create("Sys");
            var serializer = new ProtobufSerializer((ExtendedActorSystem)system);
            var actor = system.ActorOf<DummyActor>("TestActor");

            var obj = new ResponseMessage
            {
                ReturnPayload = new ActorReturnMessage { v = actor }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ReturnPayload.Value, obj2.ReturnPayload.Value);
        }

        [ProtoContract, TypeAlias]
        public class NotificationChannelReturnMessage : IValueGetable
        {
            [ProtoMember(1)]
            public INotificationChannel v;

            public object Value
            {
                get { return v; }
            }
        }

        [Fact]
        public void TestSurrogateForINotificationChannel()
        {
            var system = ActorSystem.Create("Sys");
            var serializer = new ProtobufSerializer((ExtendedActorSystem)system);
            var actor = system.ActorOf<DummyActor>("TestActor");

            var obj = new ResponseMessage
            {
                ReturnPayload = new NotificationChannelReturnMessage { v = new ActorNotificationChannel(actor) }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ReturnPayload.Value, obj2.ReturnPayload.Value);
        }
    }
}
