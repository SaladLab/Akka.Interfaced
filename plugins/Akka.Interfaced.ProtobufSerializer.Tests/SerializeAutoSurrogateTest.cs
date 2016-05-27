using System;
using ProtoBuf;
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

            [ProtoConverter]
            public static SourceSurrogate Convert(Source source)
            {
                return new SourceSurrogate { Data = source.Data };
            }

            [ProtoConverter]
            public static Source Convert(SourceSurrogate surrogate)
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

            [ProtoConverter]
            public static Source2_ Convert(Source2 source)
            {
                return new Source2_ { Data = source.Data };
            }

            [ProtoConverter]
            public static Source2 Convert(Source2_ surrogate)
            {
                return new Source2 { Data = surrogate.Data };
            }
        }

        [ProtoContract]
        public class ProtobufSurrogateDirectives
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
