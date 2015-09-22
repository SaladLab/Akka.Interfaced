using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;

namespace Akka.Interfaced.SlimSocketBase
{
    internal struct StreamLengthMarker
    {
        private readonly Stream _stream;
        private readonly long _startPosition;
        private long _endPosition;

        public StreamLengthMarker(Stream stream, bool moveToNext)
        {
            _stream = stream;
            _startPosition = _stream.Position;
            _endPosition = 0;

            if (moveToNext)
                _stream.Seek(4, SeekOrigin.Current);
        }

        public long StartPosition
        {
            get { return _startPosition; }
        }

        public long EndPosition
        {
            get { return _endPosition; }
        }

        public int Length
        {
            get { return (int)(_endPosition - _startPosition - 4); }
        }

        public void WriteLength(bool recoverStreamPosition)
        {
            _endPosition = _stream.Position;
            var len = Length;
            _stream.Seek(_startPosition, SeekOrigin.Begin);
            var lenBytes = BitConverter.GetBytes(len);
            _stream.Write(lenBytes, 0, lenBytes.Length);

            if (recoverStreamPosition)
                _stream.Seek(_endPosition, SeekOrigin.Begin);
        }
    }
}
