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
        public void TestSurrogateActorPath()
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
        public void TestSurrogateAddress()
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
    }
}
