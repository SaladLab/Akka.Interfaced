using Akka.Actor;
using ProtoBuf;
using TypeAlias;
using Xunit;

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    public class SerializeSurrogateTest
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

            var obj = new ReplyMessage
            {
                RequestId = 12345678,
                Result = new PathReturnMessage { v = ActorPath.Parse("akka://system@host:1234/user/protobuf") }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ReplyMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.Result.Value.ToString(), obj2.Result.Value.ToString());
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

            var obj = new ReplyMessage
            {
                RequestId = 12345678,
                Result = new AddressReturnMessage { v = Address.Parse("akka://system@host:1234") }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ReplyMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.Result.Value.ToString(), obj2.Result.Value.ToString());
        }
    }
}
