using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;

namespace SlimUnity.Interface
{
    [ProtoContract, TypeAlias]
    public class CounterException : Exception
    {
        [ProtoMember(1)] public int Code;

        public CounterException()
        {
        }

        public CounterException(int code)
        {
            Code = code;
        }

        public override string ToString()
        {
            return string.Format("CounterException(code={0})", Code);
        }
    }

    public interface ICounter : IInterfacedActor
    {
        Task IncCounter(int delta);
        Task<int> GetCounter();
    }
}
