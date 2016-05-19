using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    // Helper for creating delegates
    // http://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/

    // Convert
    //    void T.Method(object)
    // -> MessageHandler
    internal static class MessageHandlerFuncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(MessageHandlerFuncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static MessageHandler Build(Type targetType, MethodInfo method)
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                targetType, method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (MessageHandler)ret;
        }

        private static MessageHandler BuildHelper<TTarget, TParam>(MethodInfo method)
            where TTarget : class
        {
            var func = (Action<TTarget, TParam>)Delegate.CreateDelegate(
                typeof(Action<TTarget, TParam>), method);

            return (target, param) =>
            {
                func((TTarget)target, (TParam)param);
            };
        }
    }

    // Convert
    //    Task T.Method(object)
    // -> MessageAsyncHandler
    internal static class MessageHandlerAsyncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(MessageHandlerAsyncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static MessageAsyncHandler Build(Type targetType, MethodInfo method)
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                targetType, method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (MessageAsyncHandler)ret;
        }

        private static MessageAsyncHandler BuildHelper<TTarget, TParam>(MethodInfo method)
        {
            var func = (Func<TTarget, TParam, Task>)Delegate.CreateDelegate(
                typeof(Func<TTarget, TParam, Task>), method);

            return (target, param) =>
            {
                return func((TTarget)target, (TParam)param);
            };
        }
    }

    // Convert
    //    void T.Method(object)
    // -> MessageAsyncHandler
    internal static class MessageHandlerSyncToAsyncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(MessageHandlerSyncToAsyncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static MessageAsyncHandler Build(Type targetType, MethodInfo method)
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                targetType, method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (MessageAsyncHandler)ret;
        }

        private static MessageAsyncHandler BuildHelper<TTarget, TParam>(MethodInfo method)
        {
            var func = (Action<TTarget, TParam>)Delegate.CreateDelegate(
                typeof(Action<TTarget, TParam>), method);

            return (target, param) =>
            {
                try
                {
                    func((TTarget)target, (TParam)param);
                    return Task.FromResult(true);
                }
                catch (Exception e)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    tcs.SetException(e);
                    return tcs.Task;
                }
            };
        }
    }
}
