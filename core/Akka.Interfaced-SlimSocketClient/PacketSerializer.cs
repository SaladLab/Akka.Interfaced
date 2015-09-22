using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Akka.Interfaced.SlimSocketBase;

namespace Akka.Interfaced.SlimSocketClient
{
    public class PacketSerializer : PacketSerializerBase
    {
        public PacketSerializer(Data data)
            : base(data)
        {
        }

        protected override void GetBuffers(
            Stream stream, int pos, int length,
            out ArraySegment<byte> segment0, out ArraySegment<byte> segment1)
        {
            var ms = stream as MemoryStream;
            if (ms != null)
            {
                segment0 = new ArraySegment<byte>(ms.GetBuffer(), pos, length);
                segment1 = new ArraySegment<byte>();
                return;
            }
            throw new InvalidOperationException("Unknown stream!");
        }
    }
}
