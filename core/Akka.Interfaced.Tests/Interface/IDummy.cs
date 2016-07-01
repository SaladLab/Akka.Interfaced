using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IDummy : IInterfacedActor
    {
        Task<object> Call(object param);
    }

    public interface IDummyEx : IDummy
    {
        Task<object> CallEx(object param);
    }

    public interface IDummyEx2 : IDummy
    {
        Task<object> CallEx2(object param);
    }

    public interface IDummyExFinal : IDummyEx, IDummyEx2
    {
        Task<object> CallExFinal(object param);
    }

    public interface IDummy<T> : IInterfacedActor
        where T : ICloneable
    {
        Task<T> Call(T param);
        Task<T> Call<U>(T param, U param2)
            where U : IComparable<U>;
    }
}
