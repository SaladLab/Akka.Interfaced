using System;
using System.IO;
using ProtoBuf.Meta;

namespace Akka.Interfaced.SlimSocketBase
{
    public class ProtoBufMessageSerializer : IMessageSerializer
    {
        private readonly TypeModel _model;

        public ProtoBufMessageSerializer(TypeModel model)
        {
            _model = model;
        }

        public bool CanSerialize(Type type)
        {
            return _model.CanSerialize(type);
        }

        public void Serialize(Stream dest, object value)
        {
            _model.Serialize(dest, value);
        }

        public object Deserialize(Stream source, object value, Type type)
        {
            return _model.Deserialize(source, value, type);
        }

        public object Deserialize(Stream source, object value, Type type, int length)
        {
            return _model.Deserialize(source, value, type, length);
        }
    }
}
