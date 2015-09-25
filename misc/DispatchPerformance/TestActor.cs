using System;
using System.Threading.Tasks;

namespace DispatchPerformance
{
    public class TestActor : ITest
    {
        Task<int> ITest.Min01(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min02(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min03(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min04(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min05(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min06(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min07(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min08(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min09(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min10(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min11(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min12(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min13(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min14(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min15(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min16(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min17(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min18(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min19(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }

        Task<int> ITest.Min20(int a, int b)
        {
            return Task.FromResult(Math.Min(a, b));
        }
    }

    public class TestActorRaw
    {
        public int Min01(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min02(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min03(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min04(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min05(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min06(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min07(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min08(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min09(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min10(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min11(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min12(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min13(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min14(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min15(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min16(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min17(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min18(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min19(int a, int b)
        {
            return Math.Min(a, b);
        }

        public int Min20(int a, int b)
        {
            return Math.Min(a, b);
        }
    }
}
