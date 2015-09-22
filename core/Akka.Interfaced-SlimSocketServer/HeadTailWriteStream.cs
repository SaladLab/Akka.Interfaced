using System;
using System.IO;

namespace Akka.Interfaced.SlimSocketServer
{
    internal class HeadTailWriteStream : Stream
    {
        private ArraySegment<byte> _head;
        private ArraySegment<byte>? _tail;
        private int _pos;
        private int _length;
        private int _size;

        public HeadTailWriteStream(ArraySegment<byte> head, int tailSize = 0)
        {
            _head = head;
            if (tailSize > 0)
                _tail = new ArraySegment<byte>(new byte[tailSize]);
            _size = head.Count + tailSize;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get { return _pos; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _pos = (int)value;
            }
        }

        public ArraySegment<byte>? Tail
        {
            get { return _tail; }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long pos;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    pos = offset;
                    break;

                case SeekOrigin.Current:
                    pos = _pos + offset;
                    break;

                case SeekOrigin.End:
                    pos = _length + offset;
                    break;

                default:
                    throw new ArgumentException("Invalid origin");
            }
            Position = pos;
            return pos;
        }

        public override void SetLength(long length)
        {
            _length = (int)length;
            EnsureCapacity(_length);
        }

        private void EnsureCapacity(int size)
        {
            if (size <= _size)
                return;

            int requiredTailSize = size - _head.Count;
            int orgTailSize = _tail != null ? _tail.Value.Count : 0;
            int newTailSize = requiredTailSize;
            if (newTailSize < 0x100)
                newTailSize = 0x100;
            if (newTailSize < orgTailSize * 2)
                newTailSize = orgTailSize * 2;

            if (_tail != null)
            {
                var newTail = new ArraySegment<byte>(new byte[newTailSize]);
                Array.Copy(_tail.Value.Array, _tail.Value.Offset, newTail.Array, newTail.Offset, orgTailSize);
                _tail = newTail;
            }
            else
            {
                _tail = new ArraySegment<byte>(new byte[newTailSize]);
            }

            _size = _head.Count + newTailSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int posEnd = _pos + count;
            EnsureCapacity(posEnd);

            int headSize = _head.Count;
            if (posEnd <= headSize)
            {
                // 데이터가 모두 Head 에만 기록될 경우

                var headOffset = _head.Offset + _pos;
                if (count <= 8)
                {
                    for (int i = 0; i < count; i++)
                        _head.Array[headOffset + i] = buffer[offset + i];
                }
                else
                {
                    Array.Copy(buffer, offset, _head.Array, headOffset, count);
                }
            }
            else if (_pos >= headSize)
            {
                // 데이터가 모두 Tail 에만 기록될 경우

                var tailOffset = _pos - headSize + _tail.Value.Offset;
                if (count <= 8)
                {
                    for (int i = 0; i < count; i++)
                        _tail.Value.Array[tailOffset + i] = buffer[offset + i];
                }
                else
                {
                    Array.Copy(buffer, offset, _tail.Value.Array, tailOffset, count);
                }
            }
            else
            {
                // 데이터가 Head 와 Tail 에 나눠 기록될 경우

                int headPartCount = headSize - _pos;
                int tailPartCount = posEnd - headSize;
                Array.Copy(buffer, 0, _head.Array, _head.Offset + _pos, headPartCount);
                Array.Copy(buffer, headPartCount, _tail.Value.Array, _tail.Value.Offset, tailPartCount);
            }

            _pos = posEnd;
            if (_length < posEnd)
                _length = posEnd;
        }

        public void GetBuffers(int pos, int length, out ArraySegment<byte> segment0, out ArraySegment<byte> segment1)
        {
            var posEnd = pos + length;
            if (pos < 0 || posEnd > _length)
                throw new ArgumentOutOfRangeException();

            if (pos < _head.Count && posEnd <= _head.Count)
            {
                segment0 = new ArraySegment<byte>(_head.Array, _head.Offset + pos, length);
                segment1 = new ArraySegment<byte>();
            }
            else if (pos >= _head.Count && posEnd >= _head.Count)
            {
                segment0 = new ArraySegment<byte>(_tail.Value.Array, _tail.Value.Offset + pos - _head.Count, length);
                segment1 = new ArraySegment<byte>();
            }
            else
            {
                var firstLen = _head.Count - pos;
                segment0 = new ArraySegment<byte>(_head.Array, _head.Offset + pos, firstLen);
                segment1 = new ArraySegment<byte>(_tail.Value.Array, _tail.Value.Offset, length - firstLen);
            }
        }
    }
}
