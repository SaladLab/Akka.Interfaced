using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface IOverloaded : IInterfacedActor
    {
        Task<int> Min(int a, int b);
        Task<int> Min(int a, int b, int c);
        Task<int> Min(params int[] nums);
    }
}
