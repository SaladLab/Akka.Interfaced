using System;
using System.IO;

namespace Akka.Interfaced.SlimSocketBase
{
    public interface IMessageSerializer
    {
        bool CanSerialize(Type type);
        void Serialize(Stream dest, object value);
        object Deserialize(Stream source, object value, Type type);
        object Deserialize(Stream source, object value, Type type, int length);
    }
}
