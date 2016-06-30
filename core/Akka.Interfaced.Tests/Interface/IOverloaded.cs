using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IOverloaded : IInterfacedActor
    {
        Task<int> Min(int a, int b);
        Task<int> Min(int a, int b, int c);
        Task<int> Min(params int[] nums);
    }

    // Overloaded generic methods
    public interface IOverloadedGeneric : IInterfacedActor
    {
        Task<T> Min<T>(T a, T b)
            where T : IComparable<T>;
        Task<T> Min<T>(T a, T b, T c)
            where T : IComparable<T>;
        Task<T> Min<T>(params T[] nums)
            where T : IComparable<T>;
    }

    // Overloaded generic methods in generic interface
    public interface IOverloadedGeneric<T> : IInterfacedActor
        where T : new()
    {
        Task<T> GetDefault();
        Task<U> Min<U>(U a, U b)
            where U : IComparable<U>;
        Task<U> Min<U>(U a, U b, U c)
            where U : IComparable<U>;
        Task<U> Min<U>(params U[] nums)
            where U : IComparable<U>;
    }
}
