using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using SlimHttp.Interface;

namespace SlimHttp.Program.Server
{
    [ResponsiveException(typeof(ArgumentNullException))]
    public class CalculatorActor : InterfacedActor, ICalculator
    {
        Task<string> ICalculator.Concat(string a, string b)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (b == null) throw new ArgumentNullException("b");
            return Task.FromResult(a + b);
        }

        Task<int> ICalculator.Sum(int a, int b)
        {
            return Task.FromResult(a + b);
        }
    }
}
