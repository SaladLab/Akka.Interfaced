using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using SlimHttp.Interface;

#pragma warning disable 1998

namespace SlimHttp.Program.Server
{
    public class PedanticActor : InterfacedActor, IPedantic
    {
        async Task IPedantic.TestCall()
        {
        }

        async Task<int?> IPedantic.TestOptional(int? value)
        {
            return value;
        }

        async Task<Tuple<int, string>> IPedantic.TestTuple(Tuple<int, string> value)
        {
            return value;
        }

        async Task<int[]> IPedantic.TestParams(params int[] values)
        {
            return values;
        }

        async Task<string> IPedantic.TestPassClass(TestParam param)
        {
            return string.Format("{0}:{1}", param.Name, param.Price);
        }

        async Task<TestResult> IPedantic.TestReturnClass(int value, int offset)
        {
            return new TestResult { Value = value, Offset = offset };
        }
    }
}

#pragma  warning restore 1998
