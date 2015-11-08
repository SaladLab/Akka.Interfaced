using System.Threading.Tasks;
using Akka.Interfaced;

namespace Basic.Interface
{
    public interface ICalculator : IInterfacedActor
    {
        Task<string> Concat(string a, string b);
        Task<int> Sum(int a, int b);
    }
}
