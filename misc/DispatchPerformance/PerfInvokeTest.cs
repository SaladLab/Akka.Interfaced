using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DispatchPerformance
{
    static class PerfInvokeTest
    {
        const int TestCount = 10000000;

        public static void Run()
        {
            for (int i = 0; i < 1; i++)
            {
                DoNothing();
                DoCallDirectRaw();
                DoCallDirectAsync();
                DoCallRaw();
                DoCallAsync();
                DoCallContinueAsync();
                DoCallFuncRaw();
                DoCallFuncAsync();
                DoCallMethodInvokeRaw();
                DoCallMethodInvokeAsync();
                DoCallCompiledRaw();
                DoCallCompiledAsync();
            }
        }

        private static void DoNothing()
        {
            RunTest<TestActor>("DoNothing", (actor, msg) =>
            {
            });
        }

        private static void DoCallDirectRaw()
        {
            var r = 0;
            RunTest<TestActorRaw>("DoCallDirectRaw", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += (actor).Min01(m.a, m.b);
            });
        }

        private static void DoCallDirectAsync()
        {
            var r = 0;
            RunTest<TestActor>("DoCallDirectAsync", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += ((ITest)actor).Min01(m.a, m.b).Result;
            });
        }

        private static void DoCallRaw()
        {
            var r = 0;
            RunTest<TestActorRaw>("DoCallRaw", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += (int)m.Invoke_Raw(actor).Value;
            });
        }

        private static void DoCallAsync()
        {
            var r = 0;
            RunTest<TestActor>("DoCallAsync", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += (int)m.Invoke(actor).Result.Value;
            });
        }

        private static void DoCallContinueAsync()
        {
            var r = 0;
            var methodInfo = typeof(TestActor).GetInterfaceMap(typeof(ITest)).TargetMethods[0];
            var handler = CreateDelegate<TestActor, ITest__Min00__Invoke>(methodInfo);
            RunTest<TestActor>("DoCallContinueAsync", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += m.Invoke_Continue(actor).Result != null ? 1 : 0;
            });
        }

        private static void DoCallFuncRaw()
        {
            var r = 0;
            var methodInfo = typeof(TestActorRaw).GetMethod("Min01");
            var targetMethod = (Func<TestActorRaw, int, int, int>)
                Delegate.CreateDelegate(typeof (Func<TestActorRaw, int, int, int>), methodInfo);
            RunTest<TestActorRaw>("DoCallFuncRaw", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += (int)m.Call(actor, targetMethod).Value;
            });
        }

        private static void DoCallFuncAsync()
        {
            var r = 0;
            var methodInfo = typeof(TestActor).GetInterfaceMap(typeof(ITest)).TargetMethods[0];
            var targetMethod = (Func<TestActor, int, int, Task<int>>)
                Delegate.CreateDelegate(typeof(Func<TestActor, int, int, Task<int>>), methodInfo);
            RunTest<TestActor>("DoCallFuncAsync", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += (int)m.CallAsync(actor, targetMethod).Result.Value;
            });
        }

        private static void DoCallMethodInvokeRaw()
        {
            var r = 0;
            var methodInfo = typeof(TestActorRaw).GetMethod("Min01");
            RunTest<TestActorRaw>("DoCallMethodInvokeRaw", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += (int)methodInfo.Invoke(actor, new object[] { m.a, m.b });
            });
        }

        private static void DoCallMethodInvokeAsync()
        {
            var r = 0;
            var methodInfo = typeof(TestActor).GetInterfaceMap(typeof(ITest)).TargetMethods[0];
            RunTest<TestActor>("DoCallMethodInvokeAsync", (actor, msg) =>
            {
                var m = (ITest__Min00__Invoke)msg;
                r += ((Task<int>)methodInfo.Invoke(actor, new object[] { m.a, m.b })).Result;
            });
        }

        private static void DoCallCompiledAsync()
        {
            var r = 0;
            var methodInfo = typeof(TestActor).GetInterfaceMap(typeof(ITest)).TargetMethods[0];
            var handler = CreateDelegate<TestActor, ITest__Min00__Invoke>(methodInfo);
            RunTest<TestActor>("DoCallCompiledAsync", (actor, msg) =>
            {
                r += handler(actor, msg).Result;
            });
        }

        private static Func<TTarget, object, Task<int>> CreateDelegate<TTarget, TRequest>(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var argument = Expression.Parameter(typeof(object), "argument");

            var varExpr = Expression.Variable(typeof(TRequest), "msg");
            var prop1 = Expression.Field(varExpr, "a");
            var prop2 = Expression.Field(varExpr, "b");

            var blockExpr = Expression.Block(
                new ParameterExpression[] {varExpr},
                Expression.Assign(varExpr, Expression.Convert(argument, typeof (TRequest))),
                Expression.Call(instance, method, prop1, prop2));

            return Expression.Lambda<Func<TTarget, object, Task<int>>>(
                Expression.Convert(blockExpr, typeof(Task<int>)),
                instance, argument
            ).Compile();
        }

        private static void DoCallCompiledRaw()
        {
            var r = 0;
            var methodInfo = typeof(TestActorRaw).GetMethod("Min01");
            var handler = CreateDelegateRaw<TestActorRaw, ITest__Min00__Invoke>(methodInfo);
            RunTest<TestActorRaw>("DoCallCompiledRaw", (actor, msg) =>
            {
                r += handler(actor, msg);
            });
        }

        private static Func<TTarget, object, int> CreateDelegateRaw<TTarget, TRequest>(MethodInfo method)
        {
            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var argument = Expression.Parameter(typeof(object), "argument");

            var varExpr = Expression.Variable(typeof(TRequest), "msg");
            var prop1 = Expression.Field(varExpr, "a");
            var prop2 = Expression.Field(varExpr, "b");

            var blockExpr = Expression.Block(
                new ParameterExpression[] { varExpr },
                Expression.Assign(varExpr, Expression.Convert(argument, typeof(TRequest))),
                Expression.Call(instance, method, prop1, prop2));

            return Expression.Lambda<Func<TTarget, object, int>>(
                Expression.Convert(blockExpr, typeof(int)),
                instance, argument
            ).Compile();
        }

        private static void RunTest<T>(string testName, Action<T, object> test) 
            where T : new()
        {
            var actor = new T();
            var msg = new ITest__Min00__Invoke {a = 1, b = 2};

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < TestCount; i++)
            {
                test(actor, msg);
            }
            sw.Stop();

            var elapsed = sw.ElapsedMilliseconds;
            var unit = (double)elapsed * 1000000 / TestCount;
            Console.WriteLine($"{testName,-30} {elapsed,6} ms {unit,2} ps");
        }
    }
}
