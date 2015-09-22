using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace SlimHttp.Interface
{
    public class TestParam
    {
        public string Name;
        public int Price;

        public override string ToString()
        {
            return string.Format("TestParam {{ Name={0}, Price={1} }}", Name, Price);
        }
    }

    public class TestResult
    {
        public int Value;
        public int Offset;

        public override string ToString()
        {
            return string.Format("TestResult {{ Value={0}, Offset={1} }}", Value, Offset);
        }
    }

    public interface IPedantic : IInterfacedActor
    {
        Task TestCall();
        Task<int?> TestOptional(int? value);
        Task<Tuple<int, string>> TestTuple(Tuple<int, string> value);
        Task<int[]> TestParams(params int[] values);
        Task<string> TestPassClass(TestParam param);
        Task<TestResult> TestReturnClass(int value, int offset);
    }
}
