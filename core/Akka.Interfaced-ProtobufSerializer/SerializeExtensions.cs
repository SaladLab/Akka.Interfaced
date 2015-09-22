using System;
using System.IO;
using System.Text;

namespace Akka.Interfaced.ProtobufSerializer
{
    public static class SerializeExtensions
    {
        public static void Write32BitEncodedInt(this Stream stream, int value)
        {
            // Naive implementation
            var bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static int Read32BitEncodedInt(this Stream stream)
        {
            // Naive implementation
            var intBytes = new byte[4];
            stream.Read(intBytes, 0, 4);
            return BitConverter.ToInt32(intBytes, 0);
        }

        public static void Write7BitEncodedInt(this Stream stream, int value)
        {
            do
            {
                int high = (value >> 7) & 0x01ffffff;
                byte b = (byte)(value & 0x7f);

                if (high != 0)
                {
                    b = (byte)(b | 0x80);
                }

                stream.WriteByte(b);
                value = high;
            } while (value != 0);
        }

        public static int Read7BitEncodedInt(this Stream stream)
        {
            int ret = 0;
            int shift = 0;
            int len;
            byte b;

            for (len = 0; len < 5; ++len)
            {
                b = (byte)stream.ReadByte();

                ret = ret | ((b & 0x7f) << shift);
                shift += 7;
                if ((b & 0x80) == 0)
                    break;
            }

            if (len < 5)
                return ret;

            throw new FormatException("Too many bytes in what should have been a 7 bit encoded Int32.");
        }

        public static void WriteZigZag7BitEncodedInt(this Stream stream, int value)
        {
            Write7BitEncodedInt(stream, (value << 1) ^ (value >> 31));
        }

        public static int ReadZigZag7BitEncodedInt(this Stream stream)
        {
            int value = Read7BitEncodedInt(stream);
            return (value >> 1) ^ (-(value & 1));
        }

        public static void WriteString(this Stream stream, string value)
        {
            if (value == null)
            {
                stream.Write7BitEncodedInt(0);
                return;
            }
            
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write7BitEncodedInt(value.Length + 1);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static string ReadString(this Stream stream)
        {
            var length = stream.Read7BitEncodedInt();
            if (length == 0)
            {
                return null;
            }
            else if (length == 1)
            {
                return string.Empty;
            }
            else
            {
                var bytes = new byte[length - 1];
                stream.Read(bytes, 0, length - 1);
                return Encoding.UTF8.GetString(bytes);
            }
        }
    }
}
