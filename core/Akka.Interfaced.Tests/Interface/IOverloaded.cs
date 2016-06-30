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

    public interface IOverloadedGeneric : IInterfacedActor
    {
        Task<T> Min<T>(T a, T b)
            where T : IComparable<T>;
        Task<T> Min<T>(T a, T b, T c)
            where T : IComparable<T>;
        Task<T> Min<T>(params T[] nums)
            where T : IComparable<T>;
    }
}
