using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ProtoBuf;
using TypeAlias;
using Xunit;

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    public class SerializeBasicTest
    {
        [ProtoContract, TypeAlias]
        public class TestInvokableMessage : IAsyncInvokable
        {
            [ProtoMember(1)] public System.String a;
            [ProtoMember(2)] public System.String b;

            public Task<IValueGetable> Invoke(object target)
            {
                return Task.FromResult<IValueGetable>(null);
            }
        }

        [ProtoContract, TypeAlias]
        public class TestReturnMessage : IValueGetable
        {
            [ProtoMember(1)] public System.String v;

            public object Value
            {
                get { return v; }
            }
        }

        [ProtoContract, TypeAlias]
        public class TestNotificationMessage : IInvokable
        {
            [ProtoMember(1)] public System.String a;

            public void Invoke(object target)
            {
            }
        }

        [Fact]
        public void TestSeriDeseri_RequestMessage()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new RequestMessage
            {
                RequestId = 12345678,
                InvokePayload = new TestInvokableMessage { a = "Hello", b = "World" }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (RequestMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.RequestId, obj2.RequestId);
            Assert.Equal(((TestInvokableMessage)obj.InvokePayload).a, ((TestInvokableMessage)obj2.InvokePayload).a);
            Assert.Equal(((TestInvokableMessage)obj.InvokePayload).b, ((TestInvokableMessage)obj2.InvokePayload).b);
        }

        [Fact]
        public void TestSeriDeseri_ReplyMessageWithNothing()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                RequestId = 12345678,
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.RequestId, obj2.RequestId);
        }

        [Fact]
        public void TestSeriDeseri_ReplyMessageWithException()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                RequestId = 12345678,
                Exception = new Exception("Test-Exception")
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.RequestId, obj2.RequestId);
            Assert.NotNull(obj2.Exception);
        }

        [Fact]
        public void TestSeriDeseri_ReplyWithResult()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                RequestId = 12345678,
                ReturnPayload = new TestReturnMessage { v = "HelloWorld" }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, typeof(TestReturnMessage));
            Assert.Equal(obj.RequestId, obj2.RequestId);
            Assert.Equal(((TestReturnMessage)obj.ReturnPayload).Value, ((TestReturnMessage)obj2.ReturnPayload).Value);
        }

        [Fact]
        public void TestSeriDeseri_NotificationMessage()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new NotificationMessage
            {
                ObserverId = 10,
                InvokePayload = new TestNotificationMessage { a = "Namaste" }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (NotificationMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ObserverId, obj2.ObserverId);
            Assert.Equal(((TestNotificationMessage)obj.InvokePayload).a, ((TestNotificationMessage)obj2.InvokePayload).a);
        }
    }
}
