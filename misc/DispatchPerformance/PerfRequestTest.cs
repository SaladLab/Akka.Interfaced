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
    // This is a delegate that I want to check performance of
    // - public delegate Task<IValueGetable> RequestMessageHandler<in T>(T self, RequestMessage requestMessage);

    static class PerfRequestTest
    {
        public static void Run()
        {
            RunScenario1();
            RunScenario2();
            RunScenario3();
        }

        //----------------------------------------------------------------------------- RunScenario1

        public class TestActor1
        {
            public int Min(int a, int b)
            {
                return Math.Min(a, b);
            }

            public Task<int> MinAsync(int a, int b)
            {
                return Task.FromResult(Math.Min(a, b));
            }
        }

        public class TestMessage1
        {
            public int a;
            public int b;
        }

        // Scenario1 tests
        // - Direct sync call 
        // - Direct async cal
        // - Indirect sync call
        // - Indirect async call
        private static void RunScenario1()
        {
            const int TestCount = 100000000;
            var testMessage = new TestMessage1 { a = 1, b = 2 };

            Console.WriteLine("***** PerfRequestTest-1 *****\n");

            RunTest("Nothing", TestCount, testMessage, (TestActor1 actor, object message) =>
            {
                return null;
            });

            RunTest("Direct", TestCount, testMessage, (TestActor1 actor, object message) =>
            {
                var msg = (TestMessage1)message;
                var result = actor.Min(msg.a, msg.b);
                return new Temp__Result { v = result };
            });

            RunTest("DirectAsync", TestCount, testMessage, (TestActor1 actor, object message) =>
            {
                var msg = (TestMessage1)message;
                var result = actor.MinAsync(msg.a, msg.b).Result;
                return new Temp__Result { v = result };
            });

            var minMethod = (Func<TestActor1, int, int, int>)Delegate.CreateDelegate(typeof(Func<TestActor1, int, int, int>), typeof(TestActor1).GetMethod("Min"));
            RunTest("Indirect", TestCount, testMessage, (TestActor1 actor, object message) =>
            {
                var msg = (TestMessage1)message;
                var result = minMethod(actor, msg.a, msg.b);
                return new Temp__Result { v = result };
            });

            var minAsyncMethod = (Func<TestActor1, int, int, Task<int>>)Delegate.CreateDelegate(typeof(Func<TestActor1, int, int, Task<int>>), typeof(TestActor1).GetMethod("MinAsync"));
            RunTest("IndirectAsync", TestCount, testMessage, (TestActor1 actor, object message) =>
            {
                var msg = (TestMessage1)message;
                var result = minAsyncMethod(actor, msg.a, msg.b).Result;
                return new Temp__Result { v = result };
            });

            Console.WriteLine("");
        }

        //----------------------------------------------------------------------------- RunScenario2

        public interface ITest2
        {
            int Min(int a, int b);
        }

        public class TestActor2 : ITest2
        {
            public int Min(int a, int b)
            {
                return Math.Min(a, b);
            }
        }

        public class TestMessage2 : IInvokable
        {
            public int a;
            public int b;

            public IValueGetable Invoke(object target)
            {
                var result = ((ITest2)target).Min(a, b);
                return new Temp__Result { v = result };
            }

            public IValueGetable Invoke<TTarget>(TTarget target, Func<TTarget, int, int, int> method)
            {
                var result = method(target, a, b);
                return new Temp__Result { v = result };
            }
        }

        // Scenario2 tests
        // - Call by interface method
        // - Call by interface method calling another delegate
        // - Call by method-info
        // - Call by compiled expression that calls method by method-info
        private static void RunScenario2()
        {
            const int TestCount = 100000000;
            var testMessage = new TestMessage2 { a = 1, b = 2 };

            Console.WriteLine("***** PerfRequestTest-2 *****\n");

            RunTest("Interface", TestCount, testMessage, (TestActor2 actor, object message) =>
            {
                var msg = (TestMessage2)message;
                return msg.Invoke(actor);
            });

            var minLambda = (Func<TestActor2, int, int, int>)Delegate.CreateDelegate(typeof(Func<TestActor2, int, int, int>), typeof(TestActor2).GetMethod("Min"));
            RunTest("InterfaceIndirect", TestCount, testMessage, (TestActor2 actor, object message) =>
            {
                var msg = (TestMessage2)message;
                return msg.Invoke(actor, minLambda);
            });

            RunTest("Injection", TestCount, testMessage, (TestActor2 actor, object message) =>
            {
                var msg = (TestMessage2)message;
                var result = minLambda(actor, msg.a, msg.b);
                return new Temp__Result { v = result };
            });

            var minMethod = typeof(TestActor2).GetMethod("Min");
            var handler = DelegateBuilderHandlerExtendedFunc.Build<TestActor2>(typeof(TestMessage2), typeof(Temp__Result), minMethod);
            RunTest("InjectionCompiled", TestCount, testMessage, (TestActor2 actor, object message) =>
            {
                return handler(actor, message);
            });

            Console.WriteLine("");
        }

        internal static class DelegateBuilderHandlerExtendedFunc
        {
            private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
                typeof(DelegateBuilderHandlerExtendedFunc).GetMethod(
                    "BuildHelperWithReply", BindingFlags.Static | BindingFlags.NonPublic);

            public static Func<T, object, IValueGetable> Build<T>(
                Type requestMessageType, Type replyMessageType, MethodInfo method)
                where T : class
            {
                var constructedHelper =
                    _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                            typeof(T), requestMessageType, replyMessageType, method.ReturnType);

                var ret = constructedHelper.Invoke(null, new object[] { method });
                return (Func<T, object, IValueGetable>)ret;
            }

            private static Func<TTarget, object, IValueGetable>
                BuildHelperWithReply<TTarget, TRequest, TReply, TResult>(MethodInfo method) where TTarget : class
            {
                // IValueGetable Handler(TTarget instance, object message)
                // {
                //     var msg = (TRequest)message;
                //     var result = method(instance, msg.a, msg.b, ...);
                //     var reply = new TReply();
                //     reply.v = result;
                //     return (IValueGetable)reply;  
                // }

                var instance = Expression.Parameter(typeof(TTarget), "instance");
                var message = Expression.Parameter(typeof(object), "message");
                var msgVar = Expression.Variable(typeof(TRequest), "msg");
                var parameterVars = method.GetParameters().Select(p => Expression.Field(msgVar, p.Name)).ToArray();
                var resultVar = Expression.Variable(typeof(TResult), "result");
                var replyVar = Expression.Variable(typeof(TReply), "reply");

                var body = Expression.Block(
                    new[] { msgVar, resultVar, replyVar },
                    Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                    Expression.Assign(resultVar, Expression.Call(instance, method, parameterVars)),
                    Expression.Assign(replyVar, Expression.New(typeof(TReply).GetConstructor(Type.EmptyTypes))),
                    Expression.Assign(Expression.Field(replyVar, "v"), resultVar),
                    replyVar);

                return (Func<TTarget, object, IValueGetable>)Expression.Lambda(body, instance, message).Compile();
            }
        }

        //----------------------------------------------------------------------------- RunScenario3

        public class TestActor3
        {
            public int Min(int a, int b)
            {
                return Math.Min(a, b);
            }

            public Task<int> MinAsync(int a, int b)
            {
                return Task.FromResult(Math.Min(a, b));
            }
        }

        public class TestMessage3
        {
            public int a;
            public int b;
        }

        // Scenario3 tests
        // - Async call by plain await
        // - Async call by compiled expression with an async helper function
        // - Async call by ContinueWith
        // - Async call by compiled expression with ContinueWith
        private static void RunScenario3()
        {
            const int TestCount = 10000000;
            var testMessage = new TestMessage3 { a = 1, b = 2 };

            Console.WriteLine("***** PerfRequestTest-3 *****\n");

            var minAsyncMethod1 = (Func<TestActor3, int, int, Task<int>>)Delegate.CreateDelegate(
                typeof(Func<TestActor3, int, int, Task<int>>), typeof(TestActor3).GetMethod("MinAsync"));
            Func<TestActor3, object, Task<IValueGetable>> callDelegate = async (TestActor3 instance, object message) =>
            {
                var msg = (TestMessage3)message;
                var result = await minAsyncMethod1(instance, msg.a, msg.b);
                return (IValueGetable)(new Temp__Result { v = result });
            };

            RunTest("Await", TestCount, testMessage, (TestActor3 actor, object message) =>
            {
                return callDelegate(actor, message).Result;
            });

            var minAsyncMethod2 = DelegateBuilderHandlerAwait.Build<TestActor3>(
                typeof(TestMessage3), typeof(Temp__Result), typeof(TestActor3).GetMethod("MinAsync"));
            RunTest("AwaitCompiled", TestCount, testMessage, (TestActor3 actor, object message) =>
            {
                return minAsyncMethod2(actor, message).Result;
            });

            var minAsyncMethod3 = (Func<TestActor3, int, int, Task<int>>)Delegate.CreateDelegate(
                typeof(Func<TestActor3, int, int, Task<int>>), typeof(TestActor3).GetMethod("MinAsync"));

            RunTest("ContinueWith", TestCount, testMessage, (TestActor3 actor, object message) =>
            {
                var msg = (TestMessage3)message;
                var task = minAsyncMethod3(actor, msg.a, msg.b);
                var task2 = task.ContinueWith(subTask =>
                {
                    if (subTask.IsFaulted)
                        throw task.Exception.Flatten().InnerExceptions.FirstOrDefault() ?? task.Exception;
                    if (subTask.IsCanceled)
                        throw new TaskCanceledException();
                    return (IValueGetable)(new Temp__Result { v = subTask.Result });
                }, TaskContinuationOptions.ExecuteSynchronously);
                return task2.Result;
            });

            var minAsyncMethod4 = DelegateBuilderHandlerContinueWith.Build<TestActor3>(
                typeof(TestMessage3), typeof(Temp__Result), typeof(TestActor3).GetMethod("MinAsync"));
            RunTest("ContinueWithCompiled", TestCount, testMessage, (TestActor3 actor, object message) =>
            {
                return minAsyncMethod4(actor, message).Result;
            });

            Console.WriteLine("");
        }

        // Convert 
        //    Task<TResult> T.Method(...)
        // -> Func<T, object, Task<IValueGetable>>
        internal static class DelegateBuilderHandlerAwait
        {
            private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
                typeof(DelegateBuilderHandlerAwait).GetMethod(
                    "BuildHelperWithReply", BindingFlags.Static | BindingFlags.NonPublic);

            private static readonly MethodInfo _afterTaskAsyncMethodInfo =
                typeof(DelegateBuilderHandlerAwait).GetMethod(
                    "AfterTaskAsync", BindingFlags.Static | BindingFlags.NonPublic);

            public static Func<T, object, Task<IValueGetable>> Build<T>(
                Type requestMessageType, Type replyMessageType, MethodInfo method)
                where T : class
            {
                var constructedHelper =
                    _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                        typeof(T), requestMessageType, replyMessageType, method.ReturnType.GetGenericArguments()[0]);

                var ret = constructedHelper.Invoke(null, new object[] { method });
                return (Func<T, object, Task<IValueGetable>>)ret;
            }

            private static async Task<IValueGetable> AfterTaskAsync<TResult, TReply>(Task<TResult> task, Func<TResult, IValueGetable> replyWrapper)
            {
                var result = await task.ConfigureAwait(false);
                return replyWrapper(result);
            }

            private static Func<TTarget, object, Task<IValueGetable>>
                BuildHelperWithReply<TTarget, TRequest, TReply, TResult>(MethodInfo method) where TTarget : class
            {
                var afterTaskAsync = _afterTaskAsyncMethodInfo.MakeGenericMethod(
                    typeof(TResult), typeof(TReply));

                // Task<IValueGetable> Handler(TTarget instance, object message)
                // {
                //     var msg = (TRequest)message;
                //     Task<TResult> task = method(instance, msg.a, msg.b, ...);
                //     var replyWrapper = (result) => 
                //     {
                //          var reply = new TReply();
                //          reply.v = task.Result;
                //          return (IValueGetable)reply;  
                //     }
                //     return AfterTaskAsync(task, replyWrapper);
                // }

                var instance = Expression.Parameter(typeof(TTarget), "instance");
                var message = Expression.Parameter(typeof(object), "message");
                var msgVar = Expression.Variable(typeof(TRequest), "msg");
                var taskVar = Expression.Variable(typeof(Task<TResult>), "task");
                var replyWrapperVar = Expression.Variable(typeof(Func<TResult, IValueGetable>), "replyWrapper");
                var parameterVars = method.GetParameters().Select(p => Expression.Field(msgVar, p.Name)).ToArray();

                var result = Expression.Parameter(typeof(TResult), "result");
                var replyVar = Expression.Variable(typeof(TReply), "reply");
                var replyWrapperFunc = (Func<TResult, IValueGetable>)Expression.Lambda(
                    Expression.Block(
                        new[] { replyVar },
                        Expression.Assign(replyVar, Expression.New(typeof(TReply).GetConstructor(Type.EmptyTypes))),
                        Expression.Assign(Expression.Field(replyVar, "v"), result),
                        Expression.Convert(replyVar, typeof(IValueGetable))),
                    result).Compile();

                var blockExpr = Expression.Block(
                    new[] { msgVar, taskVar, replyWrapperVar },
                    Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                    Expression.Assign(taskVar, Expression.Call(instance, method, parameterVars)),
                    Expression.Assign(replyWrapperVar, Expression.Constant(replyWrapperFunc)),
                    Expression.Call(afterTaskAsync, taskVar, replyWrapperVar));

                return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(blockExpr, instance, message).Compile();
            }
        }

        // Convert 
        //    [Task|Task<TResult>] T.Method(...)
        // -> Func<T, object, Task<IValueGetable>>
        internal static class DelegateBuilderHandlerContinueWith
        {
            private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
                typeof(DelegateBuilderHandlerContinueWith).GetMethod(
                    "BuildHelperWithReply", BindingFlags.Static | BindingFlags.NonPublic);

            private static readonly MethodInfo _validateTaskMethodInfo =
                typeof(DelegateBuilderHandlerContinueWith).GetMethod(
                    "ValidateTask", BindingFlags.Static | BindingFlags.NonPublic);

            public static Func<T, object, Task<IValueGetable>> Build<T>(
                Type requestMessageType, Type replyMessageType, MethodInfo method)
                where T : class
            {
                var constructedHelper =
                    _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                        typeof(T), requestMessageType, replyMessageType, method.ReturnType.GetGenericArguments()[0]);

                var ret = constructedHelper.Invoke(null, new object[] { method });
                return (Func<T, object, Task<IValueGetable>>)ret;
            }

            private static void ValidateTask(Task task)
            {
                if (task.IsFaulted)
                    throw task.Exception.Flatten().InnerExceptions.FirstOrDefault() ?? task.Exception;
                if (task.IsCanceled)
                    throw new TaskCanceledException();
            }

            private static Func<TTarget, object, Task<IValueGetable>>
                BuildHelperWithReply<TTarget, TRequest, TReply, TResult>(MethodInfo method) where TTarget : class
            {
                // Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult>, TaskContinuationOptions);
                var taskContinueWithGenericMethod = typeof(Task<TResult>).GetMethods().Single(
                    m => m.Name == "ContinueWith" && m.GetGenericArguments().Length == 1 &&
                         m.GetParameters().Length == 2 &&
                         m.GetParameters()[0].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 1 &&
                         m.GetParameters()[1].ParameterType == typeof(TaskContinuationOptions));
                var taskContinueWithMethod = taskContinueWithGenericMethod.MakeGenericMethod(typeof(IValueGetable));

                // Task<IValueGetable> Handler(TTarget instance, object message)
                // {
                //     var msg = (TRequest)message;
                //     Task<TResult> task = method(instance, msg.a, msg.b, ...);
                //     return task.ContinueWith(
                //          subTask =>
                //          {
                //              ValidateTask(subTask);
                //              var reply = new TReply();
                //              reply.v = task.Result;
                //              return (IValueGetable)reply;  
                //          }
                //          TaskContinuationOptions.ExecuteSynchronously);
                // }

                var instance = Expression.Parameter(typeof(TTarget), "instance");
                var message = Expression.Parameter(typeof(object), "message");
                var msgVar = Expression.Variable(typeof(TRequest), "msg");
                var taskVar = Expression.Variable(typeof(Task<TResult>), "task");
                var subTaskVar = Expression.Variable(typeof(Task<TResult>), "subTask");
                var parameterVars = method.GetParameters().Select(p => Expression.Field(msgVar, p.Name)).ToArray();

                var replyVar = Expression.Variable(typeof(TReply), "reply");
                var continuation = (Func<Task<TResult>, IValueGetable>)Expression.Lambda(
                    Expression.Block(
                        new[] { replyVar },
                        Expression.Call(_validateTaskMethodInfo, subTaskVar),
                        Expression.Assign(replyVar, Expression.New(typeof(TReply).GetConstructor(Type.EmptyTypes))),
                        Expression.Assign(Expression.Field(replyVar, "v"), Expression.Property(subTaskVar, "Result")),
                        Expression.Convert(replyVar, typeof(IValueGetable))),
                    subTaskVar).Compile();

                var blockExpr = Expression.Block(
                    new[] { msgVar, taskVar },
                    Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                    Expression.Assign(taskVar, Expression.Call(instance, method, parameterVars)),
                    Expression.Call(
                        taskVar, taskContinueWithMethod,
                        Expression.Constant(continuation),
                        Expression.Constant(TaskContinuationOptions.ExecuteSynchronously)));

                return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(blockExpr, instance, message).Compile();
            }
        }

        //------------------------------------------------------------------------------------------

        private static void RunTest<T>(string testName, int testCount, object message, Func<T, object, IValueGetable> test)
            where T : new()
        {
            var actor = new T();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < testCount; i++)
            {
                test(actor, message);
            }
            sw.Stop();

            var elapsed = sw.ElapsedMilliseconds;
            var unit = (double)elapsed * 1000000 / testCount;
            Console.WriteLine($"{testName,-30} {elapsed,6} ms {unit,2} ps");
        }
    }
}
