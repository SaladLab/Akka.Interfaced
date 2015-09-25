using System;
using System.Threading.Tasks;

namespace DispatchPerformance
{
    public interface ITest
    {
        Task<int> Min01(int a, int b);
        Task<int> Min02(int a, int b);
        Task<int> Min03(int a, int b);
        Task<int> Min04(int a, int b);
        Task<int> Min05(int a, int b);
        Task<int> Min06(int a, int b);
        Task<int> Min07(int a, int b);
        Task<int> Min08(int a, int b);
        Task<int> Min09(int a, int b);
        Task<int> Min10(int a, int b);
        Task<int> Min11(int a, int b);
        Task<int> Min12(int a, int b);
        Task<int> Min13(int a, int b);
        Task<int> Min14(int a, int b);
        Task<int> Min15(int a, int b);
        Task<int> Min16(int a, int b);
        Task<int> Min17(int a, int b);
        Task<int> Min18(int a, int b);
        Task<int> Min19(int a, int b);
        Task<int> Min20(int a, int b);
    }

    public static class ITest__MessageTable
    {
        public static Type[,] GetMessageTypes()
        {
            return new Type[,]
            {
                {typeof(ITest__Min01__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min02__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min03__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min04__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min05__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min06__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min07__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min08__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min09__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min10__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min11__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min12__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min13__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min14__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min15__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min16__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min17__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min18__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min19__Invoke), typeof(Temp__Result)},
                {typeof(ITest__Min20__Invoke), typeof(Temp__Result)},
            };
        }
    }

    public class Temp__Result : IInterfacedMessage, IValueGetable
    {
        public int v;

        public Type GetInterfaceType() { return typeof(ITest); }

        public object Value { get { return v; } }
    }

    public class ITest__Min00__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min01(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }

        public IValueGetable Invoke_Raw(TestActorRaw target)
        {
            var __v = target.Min01(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }

        public Task<IValueGetable> Invoke_Continue(object target)
        {
            return ((ITest)target).Min01(a, b).ContinueWith(
                t => (IValueGetable)(new Temp__Result {v = t.Result}),
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public IValueGetable Call<TTarget>(TTarget target, Func<TTarget, int, int, int> method)
        {
            var __v = method(target, a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }

        public async Task<IValueGetable> CallAsync<TTarget>(TTarget target, Func<TTarget, int, int, Task<int>> method)
        {
            var __v = await method(target, a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min01__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min01(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min02__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min02(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min03__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min03(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min04__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min04(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min05__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min05(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min06__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min06(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min07__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min07(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min08__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min08(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min09__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min09(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min10__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min10(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min11__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min11(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min12__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min12(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min13__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min13(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min14__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min14(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min15__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min15(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min16__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min16(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min17__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min17(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min18__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min18(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min19__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min19(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

    public class ITest__Min20__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min20(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }

}
