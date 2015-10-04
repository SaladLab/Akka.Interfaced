using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Akka.Interfaced
{
    // Convert 
    //    [void|TReturn] T.Method(...)
    // -> Func<T, object, IValueGetable>
    internal static class RequestHandlerFuncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(RequestHandlerFuncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
            typeof(RequestHandlerFuncBuilder).GetMethod(
                "BuildHelperWithReturn", BindingFlags.Static | BindingFlags.NonPublic);

        public static Func<TTarget, object, IValueGetable> Build<TTarget>(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method)
            where TTarget : class
        {
            var constructedHelper =
                returnPayloadType == null
                    ? _buildHelperMethodInfo.MakeGenericMethod(
                        typeof(TTarget), invokePayloadType)
                    : _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                        typeof(TTarget), invokePayloadType, method.ReturnType, returnPayloadType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (Func<TTarget, object, IValueGetable>)ret;
        }

        private static Func<TTarget, object, IValueGetable>
            BuildHelper<TTarget, TInvokePayload>(MethodInfo method) where TTarget : class
        {
            // IValueGetable Handler(TTarget instance, object invokePayload)
            // {
            //      var invoke = (TInvokePayload)invokePayload;
            //      method(instance, invoke.a, invoke.b, ...);
            //      return null;
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var invokePayload = Expression.Parameter(typeof(object), "invokePayload");
            var invoke = Expression.Variable(typeof(TInvokePayload), "invoke");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(invoke, p.Name)).ToArray();

            var body = Expression.Block(
                new[] { invoke },
                Expression.Assign(invoke, Expression.Convert(invokePayload, typeof(TInvokePayload))),
                Expression.Call(instance, method, parameterVars),
                Expression.Constant(null, typeof(IValueGetable)));

            return (Func<TTarget, object, IValueGetable>)Expression.Lambda(body, instance, invokePayload).Compile();
        }

        private static Func<TTarget, object, IValueGetable>
            BuildHelperWithReturn<TTarget, TInvokePayload, TReturn, TReturnPayload>(MethodInfo method) where TTarget : class
        {
            // IValueGetable Handler(TTarget instance, object invokePayload)
            // {
            //      var invoke = (TInvokePayload)invokePayload;
            //      var returnValue = method(instance, invoke.a, invoke.b, ...);
            //      var returnPayload = new TReturnPayload();
            //      returnPayload.v = returnValue;
            //      return (IValueGetable)returnPayload;  
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var invokePayload = Expression.Parameter(typeof(object), "invokePayload");
            var invoke = Expression.Variable(typeof(TInvokePayload), "invoke");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(invoke, p.Name)).ToArray();
            var returnValue = Expression.Variable(typeof(TReturn), "returnValue");
            var returnPayload = Expression.Variable(typeof(TReturnPayload), "returnPayload");
            var returnPayloadValueType = typeof(TReturnPayload).GetField("v").FieldType;

            var body = Expression.Block(
                new[] { invoke, returnValue, returnPayload },
                Expression.Assign(invoke, Expression.Convert(invokePayload, typeof(TInvokePayload))),
                Expression.Assign(returnValue, Expression.Call(instance, method, parameterVars)),
                Expression.Assign(returnPayload, Expression.New(typeof(TReturnPayload).GetConstructor(Type.EmptyTypes))),
                Expression.Assign(
                    Expression.Field(returnPayload, "v"),
                    returnPayloadValueType != typeof(TReturn)
                        ? Expression.Convert(returnValue, returnPayloadValueType)
                        : (Expression)returnValue),
                returnPayload);

            return (Func<TTarget, object, IValueGetable>)Expression.Lambda(body, instance, invokePayload).Compile();
        }
    }

    // Convert 
    //    [Task|Task<TReturn>] T.Method(...)
    // -> Func<T, object, Task<IValueGetable>>
    internal static class RequestHandlerAsyncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(RequestHandlerAsyncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
            typeof(RequestHandlerAsyncBuilder).GetMethod(
                "BuildHelperWithReturn", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _afterTaskAsyncMethodInfo =
            typeof(RequestHandlerAsyncBuilder).GetMethod(
                "AfterTaskAsync", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _afterTaskValueAsyncMethodInfo =
            typeof(RequestHandlerAsyncBuilder).GetMethod(
                "AfterTaskValueAsync", BindingFlags.Static | BindingFlags.NonPublic);

        public static Func<TTarget, object, Task<IValueGetable>> Build<TTarget>(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method)
            where TTarget : class
        {
            var constructedHelper =
                returnPayloadType == null
                    ? _buildHelperMethodInfo.MakeGenericMethod(
                        typeof(TTarget), invokePayloadType)
                    : _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                        typeof(TTarget), invokePayloadType, method.ReturnType.GetGenericArguments()[0], returnPayloadType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (Func<TTarget, object, Task<IValueGetable>>)ret;
        }

        private static Func<TTarget, object, Task<IValueGetable>>
            BuildHelper<TTarget, TInvokePayload>(MethodInfo method) where TTarget : class
        {
            // async Task<IValueGetable> Handler(TTarget instance, object invokePayload)
            // {
            //      var invoke = (TInvokePayload)invokePayload;
            //      await method(instance, invoke.a, invoke.b, ...);
            //      return null;
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var invokePayload = Expression.Parameter(typeof(object), "invokePayload");
            var invoke = Expression.Variable(typeof(TInvokePayload), "invoke");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(invoke, p.Name)).ToArray();
            var taskVar = Expression.Variable(typeof(Task), "task");

            var body = Expression.Block(
                new[] { invoke, taskVar },
                Expression.Assign(invoke, Expression.Convert(invokePayload, typeof(TInvokePayload))),
                Expression.Assign(taskVar, Expression.Call(instance, method, parameterVars)),
                Expression.Call(_afterTaskAsyncMethodInfo, taskVar));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(body, instance, invokePayload).Compile();
        }

        private static Func<TTarget, object, Task<IValueGetable>>
            BuildHelperWithReturn<TTarget, TInvokePayload, TReturn, TReturnPayload>(MethodInfo method) where TTarget : class
        {
            var afterTaskValueAsync = _afterTaskValueAsyncMethodInfo.MakeGenericMethod(typeof(TReturn));

            // async Task<IValueGetable> Handler(TTarget instance, object invokePayload)
            // {
            //      var invoke = (TInvokePayload)invokePayload;
            //      var returnValue = await method(instance, invoke.a, invoke.b, ...);
            //      var returnPayload = new TReturnPayload();
            //      returnPayload.v = returnValue;
            //      return (IValueGetable)returnPayload;  
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var invokePayload = Expression.Parameter(typeof(object), "invokePayload");
            var invoke = Expression.Variable(typeof(TInvokePayload), "invoke");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(invoke, p.Name)).ToArray();
            var taskVar = Expression.Variable(typeof(Task<TReturn>), "task");
            var returnValue = Expression.Variable(typeof(TReturn), "returnValue");
            var returnPayload = Expression.Variable(typeof(TReturnPayload), "returnPayload");
            var returnPayloadValueType = typeof(TReturnPayload).GetField("v").FieldType;

            var returnWrapper = (Func<TReturn, IValueGetable>)Expression.Lambda(
                Expression.Block(
                    new[] { returnPayload },
                    Expression.Assign(returnPayload, Expression.New(typeof(TReturnPayload).GetConstructor(Type.EmptyTypes))),
                    Expression.Assign(
                        Expression.Field(returnPayload, "v"),
                        returnPayloadValueType != typeof(TReturn) 
                            ? Expression.Convert(returnValue, returnPayloadValueType)
                            : (Expression)returnValue),
                    returnPayload),
                returnValue).Compile();

            var body = Expression.Block(
                new[] { invoke, taskVar },
                Expression.Assign(invoke, Expression.Convert(invokePayload, typeof(TInvokePayload))),
                Expression.Assign(taskVar, Expression.Call(instance, method, parameterVars)),
                Expression.Call(afterTaskValueAsync, taskVar, Expression.Constant(returnWrapper)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(body, instance, invokePayload).Compile();
        }

        private static async Task<IValueGetable> AfterTaskAsync(Task task)
        {
            await task;
            return null;
        }

        private static async Task<IValueGetable> AfterTaskValueAsync<TReturn>(Task<TReturn> task, Func<TReturn, IValueGetable> returnWrapper)
        {
            var returnValue = await task;
            return returnWrapper(returnValue);
        }
    }

    // Convert 
    //    [void|TReturn] T.Method(...)
    // -> Func<T, object, Task<IValueGetable>>
    internal static class RequestHandlerSyncToAsyncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(RequestHandlerSyncToAsyncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _buildHelperWithReplyMethodInfo =
            typeof(RequestHandlerSyncToAsyncBuilder).GetMethod(
                "BuildHelperWithReturn", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _taskFromResultMethodInfo =
            typeof(RequestHandlerSyncToAsyncBuilder).GetMethod(
                "TaskFromResult", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo _taskFromExceptionMethodInfo =
            typeof(RequestHandlerSyncToAsyncBuilder).GetMethod(
                "TaskFromException", BindingFlags.Static | BindingFlags.NonPublic);

        public static Func<TTarget, object, Task<IValueGetable>> Build<TTarget>(
            Type invokePayloadType, Type returnPayloadType, MethodInfo method)
            where TTarget : class
        {
            var constructedHelper =
                returnPayloadType == null
                    ? _buildHelperMethodInfo.MakeGenericMethod(
                        typeof(TTarget), invokePayloadType)
                    : _buildHelperWithReplyMethodInfo.MakeGenericMethod(
                        typeof(TTarget), invokePayloadType, method.ReturnType, returnPayloadType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (Func<TTarget, object, Task<IValueGetable>>)ret;
        }

        private static Func<TTarget, object, Task<IValueGetable>>
            BuildHelper<TTarget, TInvokePayload>(MethodInfo method) where TTarget : class
        {
            // Task<IValueGetable> Handler(TTarget instance, object invokePayload)
            // {
            //      try
            //      {
            //          var invoke = (TInvokePayload)invokePayload;
            //          method(instance, invoke.a, invoke.b, ...);
            //          return Task.FromResult((IValueGetable)null);
            //      }
            //      catch (Exception e)
            //      {
            //          return Task.FromException<IValueGetable>(e);   
            //      }
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var invokePayload = Expression.Parameter(typeof(object), "invokePayload");
            var invoke = Expression.Variable(typeof(TInvokePayload), "invoke");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(invoke, p.Name)).ToArray();
            var exception = Expression.Parameter(typeof(Exception), "e");

            var body = Expression.TryCatch(
                Expression.Block(
                    new[] { invoke },
                    Expression.Assign(invoke, Expression.Convert(invokePayload, typeof(TInvokePayload))),
                    Expression.Call(instance, method, parameterVars),
                    Expression.Call(
                        _taskFromResultMethodInfo,
                        Expression.Constant(null, typeof(IValueGetable)))),
                Expression.Catch(
                    exception,
                    Expression.Call(_taskFromExceptionMethodInfo, exception)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(body, instance, invokePayload).Compile();
        }

        private static Func<TTarget, object, Task<IValueGetable>>
            BuildHelperWithReturn<TTarget, TInvokePayload, TReturn, TReturnPayload>(MethodInfo method) where TTarget : class
        {
            // Task<IValueGetable> Handler(TTarget instance, object invokePayload)
            // {
            //      try
            //      {
            //          var invoke = (TInvokePayload)invokePayload;
            //          var returnValue = method(instance, invoke.a, invoke.b, ...);
            //          var returnPayload = new TReturnPayload();
            //          returnPayload.v = returnValue;
            //          return Task.FromResult((IValueGetable)returnPayload);
            //      }
            //      catch (Exception e)
            //      {
            //          return Task.FromException<IValueGetable>(e);   
            //      }
            // }

            var instance = Expression.Parameter(typeof(TTarget), "instance");
            var invokePayload = Expression.Parameter(typeof(object), "invokePayload");
            var invoke = Expression.Variable(typeof(TInvokePayload), "invoke");
            var parameterVars = method.GetParameters().Select(p => Expression.Field(invoke, p.Name)).ToArray();
            var returnValue = Expression.Variable(typeof(TReturn), "returnValue");
            var returnPayload = Expression.Variable(typeof(TReturnPayload), "returnPayload");
            var returnPayloadValueType = typeof(TReturnPayload).GetField("v").FieldType;
            var exception = Expression.Parameter(typeof(Exception), "e");

            var body = Expression.TryCatch(
                Expression.Block(
                    new[] { invoke, returnValue, returnPayload },
                    Expression.Assign(invoke, Expression.Convert(invokePayload, typeof(TInvokePayload))),
                    Expression.Assign(returnValue, Expression.Call(instance, method, parameterVars)),
                    Expression.Assign(returnPayload, Expression.New(typeof(TReturnPayload).GetConstructor(Type.EmptyTypes))),
                    Expression.Assign(
                        Expression.Field(returnPayload, "v"),
                        returnPayloadValueType != typeof(TReturn)
                            ? Expression.Convert(returnValue, returnPayloadValueType)
                            : (Expression)returnValue),
                    Expression.Call(_taskFromResultMethodInfo, returnPayload)),
                Expression.Catch(
                    exception,
                    Expression.Call(_taskFromExceptionMethodInfo, exception)));

            return (Func<TTarget, object, Task<IValueGetable>>)Expression.Lambda(body, instance, invokePayload).Compile();
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
    }
}
