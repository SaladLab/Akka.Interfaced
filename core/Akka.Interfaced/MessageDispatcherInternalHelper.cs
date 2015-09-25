using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    // Helper for creating delegates
    // http://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/

    // Convert 
    //    Task T.Method(object)
    // -> PlainMessageHandler<T>
    internal static class DelegateBuilderSimpleTask
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(DelegateBuilderSimpleTask).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static PlainMessageHandler<T> Build<T>(MethodInfo method)
            where T : class
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                typeof(T), method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (PlainMessageHandler<T>)ret;
        }

        private static PlainMessageHandler<TTarget> BuildHelper<TTarget, TParam>(MethodInfo method)
            where TTarget : class
        {
            var func = (Func<TTarget, TParam, Task>)Delegate.CreateDelegate(
                typeof(Func<TTarget, TParam, Task>), method);

            PlainMessageHandler<TTarget> ret = (target, param) =>
            {
                return func(target, (TParam)param);
            };
            return ret;
        }
    }

    // Convert 
    //    void T.Method(object)
    // -> PlainMessageHandler<T>
    internal static class DelegateBuilderSimpleFunc
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(DelegateBuilderSimpleFunc).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static PlainMessageHandler<T> Build<T>(MethodInfo method)
            where T : class
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                typeof(T), method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (PlainMessageHandler<T>)ret;
        }

        private static PlainMessageHandler<TTarget> BuildHelper<TTarget, TParam>(MethodInfo method)
            where TTarget : class
        {
            var func = (Action<TTarget, TParam>)Delegate.CreateDelegate(
                typeof(Action<TTarget, TParam>), method);

            PlainMessageHandler<TTarget> ret = (target, param) =>
            {
                func(target, (TParam)param);
                return null;
            };
            return ret;
        }
    }

    // Convert 
    //    [Task|Task<TResult>] T.Method(...)
    // -> Func<T, object, Task<IValueGetable>>
    internal static class DelegateBuilderHandlerExtendedTask
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(DelegateBuilderHandlerExtendedTask).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
            typeof(DelegateBuilderHandlerExtendedTask).GetMethod(
                "BuildHelperWithReply", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _validateTaskMethodInfo =
            typeof(DelegateBuilderHandlerExtendedTask).GetMethod(
                "ValidateTask", BindingFlags.Static | BindingFlags.NonPublic);

        public static Func<T, object, Task<IValueGetable>> Build<T>(
            Type requestMessageType, Type replyMessageType, MethodInfo method)
            where T : class
        {
            var constructedHelper =
                replyMessageType == null
                    ? _buildHelperMethodInfo.MakeGenericMethod(
                        typeof(T), requestMessageType)
                    : _buildHelperWithReplyMethodInfo.MakeGenericMethod(
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
            BuildHelper<TTarget, TRequest>(MethodInfo method) where TTarget : class
        {
            // Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, TaskContinuationOptions);
            var taskContinueWithGenericMethod = typeof(Task).GetMethods().Single(
                m => m.Name == "ContinueWith" && m.GetGenericArguments().Length == 1 &&
                     m.GetParameters().Length == 2 &&
                     m.GetParameters()[1].ParameterType == typeof(TaskContinuationOptions));
            var taskContinueWithMethod = taskContinueWithGenericMethod.MakeGenericMethod(typeof(IValueGetable));

            // Task<IValueGetable> Handler(TTarget instance, object message)
            // {
            //     var msg = (TRequest)message;
            //     Task task = method(instance, msg.a, msg.b, ...);
            //     return task.ContinueWith(
            //          subTask => 
            //          {
            //              ValidateTask(subTask);
            //              return (IValueGetable)null; 
            //          }
            //          TaskContinuationOptions.ExecuteSynchronously);
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var message = Expression.Parameter(typeof(object), "message");
            var msgVar = Expression.Variable(typeof(TRequest), "msg");
            var taskVar = Expression.Variable(typeof(Task), "task");
            var subTaskVar = Expression.Variable(typeof(Task), "subTask");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(msgVar, p.Name)).ToArray();

            var continuation =
                Expression.Block(
                    Expression.Call(_validateTaskMethodInfo, subTaskVar),
                    Expression.Constant(null, typeof(IValueGetable)));

            var blockExpr = Expression.Block(
                new[] { msgVar, taskVar },
                Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                Expression.Assign(taskVar, Expression.Call(instance, method, parameterVars)),
                Expression.Call(
                    taskVar, taskContinueWithMethod,
                    Expression.Lambda<Func<Task, IValueGetable>>(continuation, subTaskVar),
                    Expression.Constant(TaskContinuationOptions.ExecuteSynchronously)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(blockExpr, instance, message).Compile();
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
            var continuation = Expression.Block(
                new[] { replyVar },
                Expression.Call(_validateTaskMethodInfo, subTaskVar),
                Expression.Assign(replyVar, Expression.New(typeof(TReply).GetConstructor(Type.EmptyTypes))),
                Expression.Assign(Expression.Field(replyVar, "v"),  Expression.Property(subTaskVar, "Result")),
                Expression.Convert(replyVar, typeof(IValueGetable)));

            var blockExpr = Expression.Block(
                new[] { msgVar, taskVar },
                Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                Expression.Assign(taskVar, Expression.Call(instance, method, parameterVars)),
                Expression.Call(
                    taskVar, taskContinueWithMethod,
                    Expression.Lambda<Func<Task<TResult>, IValueGetable>>(continuation, subTaskVar),
                    Expression.Constant(TaskContinuationOptions.ExecuteSynchronously)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(blockExpr, instance, message).Compile();
        }
    }

    // Convert 
    //    [void|TResult] T.Method(...)
    // -> Func<T, object, Task<IValueGetable>>
    internal static class DelegateBuilderHandlerExtendedFunc
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(DelegateBuilderHandlerExtendedFunc).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
            typeof(DelegateBuilderHandlerExtendedFunc).GetMethod(
                "BuildHelperWithReply", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _taskFromResultMethodInfo =
            typeof(DelegateBuilderHandlerExtendedFunc).GetMethod(
                "TaskFromResult", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _taskFromExceptionMethodInfo =
            typeof(DelegateBuilderHandlerExtendedFunc).GetMethod(
                "TaskFromException", BindingFlags.Static | BindingFlags.NonPublic);

        public static Func<T, object, Task<IValueGetable>> Build<T>(
            Type requestMessageType, Type replyMessageType, MethodInfo method)
            where T : class
        {
            var constructedHelper =
                replyMessageType == null
                    ? _buildHelperMethodInfo.MakeGenericMethod(
                        typeof(T), requestMessageType)
                    : _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                        typeof(T), requestMessageType, replyMessageType, method.ReturnType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (Func<T, object, Task<IValueGetable>>)ret;
        }

        private static Task<IValueGetable> TaskFromResult(IValueGetable reply)
        {
            return Task.FromResult(reply);
        }

        private static Task<IValueGetable> TaskFromException(Exception e)
        {
            // return Task.FromException<IValueGetable>(e);

            var tcs = new TaskCompletionSource<IValueGetable>();
            tcs.SetException(e);
            return tcs.Task;
        }

        private static Func<TTarget, object, Task<IValueGetable>>
            BuildHelper<TTarget, TRequest>(MethodInfo method) where TTarget : class
        {
            // Task<IValueGetable> Handler(TTarget instance, object message)
            // {
            //     try
            //     {
            //         var msg = (TRequest)message;
            //         method(instance, msg.a, msg.b, ...);
            //         return Task.FromResult((IValueGetable)null);
            //     }
            //     catch (Exception e)
            //     {
            //         return Task.FromException<IValueGetable>(e);
            //     }
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var message = Expression.Parameter(typeof(object), "message");
            var msgVar = Expression.Variable(typeof(TRequest), "msg");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(msgVar, p.Name)).ToArray();
            var exception = Expression.Parameter(typeof(Exception), "e");

            var body = Expression.TryCatch(
                Expression.Block(
                    new[] { msgVar },
                    Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                    Expression.Call(instance, method, parameterVars),
                    Expression.Call(
                        _taskFromResultMethodInfo,
                        Expression.Constant(null, typeof(IValueGetable)))),
                Expression.Catch(
                    exception,
                    Expression.Call(_taskFromExceptionMethodInfo, exception)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(body, instance, message).Compile();
        }

        private static Func<TTarget, object, Task<IValueGetable>>
            BuildHelperWithReply<TTarget, TRequest, TReply, TResult>(MethodInfo method) where TTarget : class
        {
            // Task<IValueGetable> Handler(TTarget instance, object message)
            // {
            //     try
            //     {
            //         var msg = (TRequest)message;
            //         var result = method(instance, msg.a, msg.b, ...);
            //         var reply = new TReply();
            //         reply.v = result;
            //         return (IValueGetable)reply;  
            //     }
            //     catch (Exception e)
            //     {
            //         return Task.FromException<IValueGetable>(e);
            //     }
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var message = Expression.Parameter(typeof(object), "message");
            var msgVar = Expression.Variable(typeof(TRequest), "msg");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(msgVar, p.Name)).ToArray();
            var resultVar = Expression.Variable(typeof(TResult), "result");
            var replyVar = Expression.Variable(typeof(TReply), "reply");
            var exception = Expression.Parameter(typeof(Exception), "e");

            var body = Expression.TryCatch(
                Expression.Block(
                    new[] { msgVar, resultVar, replyVar },
                    Expression.Assign(msgVar, Expression.Convert(message, typeof(TRequest))),
                    Expression.Assign(resultVar,
                                      Expression.Call(instance, method, parameterVars)),
                    Expression.Assign(replyVar, Expression.New(typeof(TReply).GetConstructor(Type.EmptyTypes))),
                    Expression.Assign(Expression.Field(replyVar, "v"), resultVar),
                    Expression.Call(
                        _taskFromResultMethodInfo,
                        Expression.Convert(replyVar, typeof(IValueGetable)))),
                Expression.Catch(
                    exception,
                    Expression.Call(_taskFromExceptionMethodInfo, exception)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(body, instance, message).Compile();
        }
    }
}
