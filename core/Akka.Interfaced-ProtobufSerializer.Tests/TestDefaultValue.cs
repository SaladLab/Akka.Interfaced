using System;
using ProtoBuf;
using TypeAlias;
using Xunit;

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    public class TestDefaultValue
    {
        [Fact]
        public void Test_Serialize_MessageContainingDefaultValues()
        {
            var serializer = new ProtobufSerializer(null);

            var msg = new IDefault_PayloadTable.CallWithDefault_Invoke { a = 2, b = 2, c = "Test" };
            var obj = new RequestMessage { InvokePayload = msg };

            var bytes = serializer.ToBinary(obj);
            var obj2 = (RequestMessage)serializer.FromBinary(bytes, null);
            var msg2 = (IDefault_PayloadTable.CallWithDefault_Invoke)obj2.InvokePayload;

            Assert.Equal(msg.a, msg2.a);
            Assert.Equal(msg.b, msg2.b);
            Assert.Equal(msg.c, msg2.c);
        }

        [Fact]
        public void Test_Check_SizeReduction_When_MessageContainingDefaultValues()
        {
            var serializer = new ProtobufSerializer(null);

            var msg1 = new IDefault_PayloadTable.Call_Invoke { a = 1, b = 2, c = "Test" };
            var msg2 = new IDefault_PayloadTable.CallWithDefault_Invoke { a = 1, b = 2, c = "Test" };

            var bytes1 = serializer.ToBinary(new RequestMessage { InvokePayload = msg1 });
            var bytes2 = serializer.ToBinary(new RequestMessage { InvokePayload = msg2 });

            Assert.InRange(bytes2.Length, 0, bytes1.Length - 1);
        }
    }
}
