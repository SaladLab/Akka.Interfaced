using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestOverloadedActor : InterfacedActor, IOverloaded
    {
        Task<int> IOverloaded.Min(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> IOverloaded.Min(int a, int b, int c)
        {
            return Task.FromResult(Math.Min(Math.Min(a, b), c));
        }

        Task<int> IOverloaded.Min(params int[] nums)
        {
            return Task.FromResult(nums.Min());
        }
    }
}
