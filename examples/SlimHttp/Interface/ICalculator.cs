using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace SlimHttp.Interface
{
    public interface ICalculator : IInterfacedActor
    {
        Task<string> Concat(string a, string b);
        Task<int> Sum(int a, int b);
    }
}
