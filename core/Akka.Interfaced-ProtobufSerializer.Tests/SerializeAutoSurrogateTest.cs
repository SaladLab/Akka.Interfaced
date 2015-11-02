using System;
using Akka.Actor;
using ProtoBuf;
using TypeAlias;
using Xunit;

namespace Akka.Interfaced.ProtobufSerializer.Tests
{
    public class SerializeAutoSurrogateTest
    {
        public class Source : IValueGetable
        {
            public string Data;

            public object Value { get { return Data; } }
        }

        [ProtoContract]
        public class SourceSurrogate
        {
            [ProtoMember(1)] public string Data;

            public static implicit operator SourceSurrogate(Source source)
            {
                return new SourceSurrogate { Data = source.Data };
            }

            public static implicit operator Source(SourceSurrogate surrogate)
            {
                return new Source { Data = surrogate.Data };
            }
        }

        [Fact]
        public void TestSourceSurrogate()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                ReturnPayload = new Source { Data = "Hello" }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ReturnPayload.Value, obj2.ReturnPayload.Value);
        }

        public class Source2 : IValueGetable
        {
            public string Data;

            public object Value { get { return Data; } }
        }

        [ProtoContract]
        public class Source2_
        {
            [ProtoMember(1)]
            public string Data;

            public static implicit operator Source2_(Source2 source)
            {
                return new Source2_ { Data = source.Data };
            }

            public static implicit operator Source2(Source2_ surrogate)
            {
                return new Source2 { Data = surrogate.Data };
            }
        }

        [ProtoContract]
        internal class ProtobufSurrogateDirectives
        {
            public Source2_ T1;
        }

        [Fact]
        public void TestSurrogateDirectives()
        {
            var serializer = new ProtobufSerializer(null);

            var obj = new ResponseMessage
            {
                ReturnPayload = new Source2 { Data = "Hello" }
            };

            var bytes = serializer.ToBinary(obj);

            var obj2 = (ResponseMessage)serializer.FromBinary(bytes, null);
            Assert.Equal(obj.ReturnPayload.Value, obj2.ReturnPayload.Value);
        }
    }
}
