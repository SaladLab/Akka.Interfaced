using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;
using TypeAlias;

namespace Akka.Interfaced.SlimSocketBase
{
    public abstract class PacketSerializerBase : IPacketSerializer
    {
        public class Data
        {
            public IMessageSerializer MessageSerializer;
            public TypeAliasTable TypeTable;

            public Data(IMessageSerializer serializer, TypeAliasTable typeTable)
            {
                MessageSerializer = serializer;
                TypeTable = typeTable;
            }
        }

        private readonly Data _data;
        private readonly ExceptionSerializer _exceptionSerializer;
        private int _serializeWrapKey;
        private int _serializeWrapPendingKey;
        private int _deserializeWrapKey;

        protected PacketSerializerBase(Data data)
        {
            _data = data;
            _exceptionSerializer = new ExceptionSerializer(data);
        }

        public void SetSerializeWrapKey(int wrapKey, bool pending = false)
        {
            if (pending)
                _serializeWrapPendingKey = wrapKey;
            else
                _serializeWrapKey = wrapKey;
        }

        public void SetDeserializeWrapKey(int wrapKey)
        {
            _deserializeWrapKey = wrapKey;
        }

        public int EstimateLength(object packet)
        {
            return 0;
        }

        // +--------+--------+--+----------+-----------+-------------+-----------+------------+-------+
        // | LEN(4) | CRC(4) |H1| ID (1~6) | AID (1~6) | M_SIG (1~6) | M_LEN (4) | M_DATA (~) | E (~) |
        // +--------+--------+--+----------+-----------+-------------+-----------+------------+-------+
        // H=[ME....TT] T=Type, M=Message?, E=Exception?
        // ID=RequestId, AID=ActorId, M=Message, E=Exception

        public void Serialize(Stream stream, object packet)
        {
            var p = (Packet)packet;

            // Jump 8 Bytes for writing Length | Checksum
            var packetLengthMarker = new StreamLengthMarker(stream, false);
            stream.Seek(8, SeekOrigin.Current);

            // Write Packet Header
            var header = (byte)((byte)(p.Type) |
                                (byte)(p.Message != null ? 0x80 : 0) |
                                (byte)(p.Exception != null ? 0x40 : 0));
            stream.WriteByte(header);
            stream.Write7BitEncodedInt(p.ActorId);
            stream.Write7BitEncodedInt(p.RequestId);

            // Write Message Length, Signature and Data
            if (p.Message != null)
            {
                var messageTypeAlias = _data.TypeTable.GetAlias(p.Message.GetType());
                stream.Write7BitEncodedInt(messageTypeAlias);
                var messageLengthMarker = new StreamLengthMarker(stream, true);
                _data.MessageSerializer.Serialize(stream, p.Message);
                messageLengthMarker.WriteLength(true);
            }

            // Write Exception
            if (p.Exception != null)
            {
                _exceptionSerializer.Serialize(stream, p.Exception);
            }

            // Write Length
            packetLengthMarker.WriteLength(false);

            // Encrypt and Calc Checksum
            ArraySegment<byte> s0, s1;
            GetBuffers(stream, (int)packetLengthMarker.StartPosition + 8, packetLengthMarker.Length - 4,
                       out s0, out s1);
            var ctx = new EncryptContext { Key = _serializeWrapKey };
            Encrypt(s0.Array, s0.Offset, s0.Array, s0.Offset, s0.Count, ref ctx);
            Encrypt(s1.Array, s1.Offset, s1.Array, s1.Offset, s1.Count, ref ctx);
            if (_serializeWrapKey != 0)
            {
                _serializeWrapKey += 1;
                if (_serializeWrapKey == 0)
                    _serializeWrapKey = 1;
            }

            // Write Checksum
            var hashBytes = BitConverter.GetBytes(ctx.Hash);
            stream.Write(hashBytes, 0, hashBytes.Length);

            // End of stream, again.
            stream.Seek(packetLengthMarker.EndPosition, SeekOrigin.Begin);

            // Pending WrapKey
            if (_serializeWrapPendingKey != 0)
            {
                _serializeWrapKey = _serializeWrapPendingKey;
                _serializeWrapPendingKey = 0;
            }
        }

        public int PeekLength(Stream stream)
        {
            var len = (int)(stream.Length - stream.Position);
            if (len < 4)
                return 0;

            // Peek Len
            var bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            stream.Seek(-4, SeekOrigin.Current);

            return BitConverter.ToInt32(bytes, 0) + 4;
        }

        public object Deserialize(Stream stream)
        {
            var p = new Packet();
            var intBytes = new byte[4];

            // Read Len and Checksum
            var startPos = stream.Position;
            int len = stream.Read32BitEncodedInt();
            int hash = stream.Read32BitEncodedInt();

            // Decrypt and Check Chcksum
            ArraySegment<byte> s0, s1;
            GetBuffers(stream, (int)startPos + 8, len - 4, out s0, out s1);
            var ctx = new DecryptContext { Key = _deserializeWrapKey };
            Decrypt(s0.Array, s0.Offset, s0.Array, s0.Offset, s0.Count, ref ctx);
            Decrypt(s1.Array, s1.Offset, s1.Array, s1.Offset, s1.Count, ref ctx);
            if (ctx.Hash != hash)
                throw new IOException("Hash mismatch");
            if (_deserializeWrapKey != 0)
            {
                _deserializeWrapKey += 1;
                if (_deserializeWrapKey == 0)
                    _deserializeWrapKey = 1;
            }

            // Read PacketType, ActorId, RequestId
            var header = stream.ReadByte();
            p.Type = (PacketType)(header & 0x3);
            p.ActorId = stream.Read7BitEncodedInt();
            p.RequestId = stream.Read7BitEncodedInt();

            // Read Message
            if ((header & 0x80) != 0)
            {
                var messageTypeAlias = stream.Read7BitEncodedInt();
                var messageLen = stream.Read32BitEncodedInt();

                Type type = _data.TypeTable.GetType(messageTypeAlias);
                if (type == null)
                    throw new Exception("Cannot resolve message type. TypeAlias=" + messageTypeAlias);

                p.Message = Activator.CreateInstance(type);
                _data.MessageSerializer.Deserialize(stream, p.Message, type, messageLen);
            }

            // Read Exception
            if ((header & 0x40) != 0)
            {
                p.Exception = _exceptionSerializer.Deserialize(stream);
            }

            var consumedLen = (int)(stream.Position - startPos);
            if (len + 4 != consumedLen)
                throw new Exception("Mismatched length: " + (len + 4) + " " + consumedLen);

            return p;
        }

        protected abstract void GetBuffers(
            Stream stream, int pos, int length,
            out ArraySegment<byte> segment0, out ArraySegment<byte> segment1);

        // EncryptChecksumHelper

        private struct EncryptContext
        {
            public int Key;
            public int Hash;
            public int Index;
        }

        private static void Encrypt(byte[] target, int targetOffset,
                                    byte[] source, int sourceOffset, int length,
                                    ref EncryptContext ctx)
        {
            int si = sourceOffset;
            int ti = targetOffset;
            for (int i = 0; i < length; i++, si++, ti++)
            {
                var p = source[si];
                ctx.Hash ^= p << ((ctx.Index % 4) * 8);
                target[ti] = (byte)(p ^ ctx.Key);
                ctx.Index += 1;
            }
        }

        private struct DecryptContext
        {
            public int Key;
            public int Hash;
            public int Index;
        }

        private static void Decrypt(byte[] target, int targetOffset,
                                    byte[] source, int sourceOffset, int length,
                                    ref DecryptContext ctx)
        {
            int si = sourceOffset;
            int ti = targetOffset;
            for (int i = 0; i < length; i++, si++, ti++)
            {
                var p = (byte)(source[si] ^ ctx.Key);
                ctx.Hash ^= p << ((ctx.Index % 4) * 8);
                target[ti] = p;
                ctx.Index += 1;
            }
        }
    }
}
