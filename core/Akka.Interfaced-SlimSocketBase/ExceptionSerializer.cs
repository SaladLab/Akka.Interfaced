using System;
using System.IO;
using TypeAlias;

namespace Akka.Interfaced.SlimSocketBase
{
    public class ExceptionSerializer
    {
        private PacketSerializerBase.Data _data;

        public ExceptionSerializer(PacketSerializerBase.Data data)
        {
            _data = data;
        }

        public void Serialize(Stream dest, Exception value)
        {
            if (dest == null) throw new ArgumentNullException("dest");
            if (value == null) throw new ArgumentNullException("value");

            var type = value.GetType();
            int typeAlias = _data.TypeTable.GetAlias(type);
            var typeSerializable = _data.MessageSerializer.CanSerialize(type);

            dest.WriteByte((byte)((typeAlias != 0 ? 1 : 0) | (typeSerializable ? 2 : 0)));

            if (typeAlias != 0)
                dest.Write32BitEncodedInt(typeAlias);
            else
                dest.WriteString(type.FullName);

            if (typeSerializable)
            {
                var lengthMarker = new StreamLengthMarker(dest, true);
                _data.MessageSerializer.Serialize(dest, value);
                lengthMarker.WriteLength(true);
            }
        }

        public Exception Deserialize(Stream source)
        {
            if (source == null) throw new ArgumentNullException("source");

            Exception exception = null;

            var flag = source.ReadByte();
            var typeAliasUsed = ((flag & 1) != 0);
            var typeSerializable = ((flag & 2) != 0);

            Type type;
            if (typeAliasUsed)
            {
                var typeAlias = source.Read32BitEncodedInt();
                type = _data.TypeTable.GetType(typeAlias);
                if (type == null)
                {
                    exception = new InvalidOperationException(
                        "Cannot resolve type from alias. Alias=" + typeAlias);
                }
            }
            else
            {
                var typeName = source.ReadString();
                type = TypeUtility.GetType(typeName);
                if (type == null)
                {
                    exception = new InvalidOperationException(
                        "Cannot resolve type from name. Name=" + typeName);
                }
            }

            if (exception == null)
            {
                exception = (Exception) Activator.CreateInstance(type);
                if (typeSerializable)
                {
                    int dataLen = source.Read32BitEncodedInt();
                    _data.MessageSerializer.Deserialize(source, exception, type, dataLen);
                }
            }
            else
            {
                if (typeSerializable)
                {
                    int dataLen = source.Read32BitEncodedInt();
                    source.Position = source.Position + dataLen;
                }
            }

            return exception;
        }
    }
}
