using System;
using System.Threading;

namespace Akka.Interfaced.SlimSocketServer
{
    internal struct InterlockedCountFlag
    {
        // LSB 1비트를 플래그로 나머지를 카운터로 사용
        private int _value;

        // 플래그
        public bool Flag
        {
            get { return (_value & 1) == 1; }
        }

        // 카운터
        public int Count
        {
            get { return _value >> 1; }
        }

        // 플래그 켬
        public bool SetFlag()
        {
            while (true)
            {
                var v = _value;
                var vNew = (v & 0x7FFFFFFE) + 1;
                if (v == vNew)
                    return false;
                if (Interlocked.CompareExchange(ref _value, vNew, v) == v)
                    return vNew == 1;
            }
        }

        // 카운터 증가 (단 Flag 가 False 일 때만 성공)
        public bool Increment()
        {
            while (true)
            {
                var v = _value;
                if ((v & 1) == 1)
                    return false;
                var vNew = v + 2;
                if (Interlocked.CompareExchange(ref _value, vNew, v) == v)
                    return true;
            }
        }

        // 카운터 감소 (Flag 와 관계 없이)
        public bool Decrement()
        {
            while (true)
            {
                var v = _value;
                if (v < 2)
                    throw new InvalidOperationException("Already Zero");
                var vNew = v - 2;
                if (Interlocked.CompareExchange(ref _value, vNew, v) == v)
                    return vNew == 1;
            }
        }

        // 플래그를 켜면서 카운터 감소 (Flag 와 관계 없이)
        public bool DecrementWithSetFlag()
        {
            while (true)
            {
                var v = _value;
                if (v < 2)
                    throw new InvalidOperationException("Already Zero");
                var vNew = ((v - 2) & 0x7FFFFFFE) + 1;
                if (Interlocked.CompareExchange(ref _value, vNew, v) == v)
                    return vNew == 1;
            }
        }
    }
}
