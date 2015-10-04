using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    // Helper for creating delegates
    // http://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/

    // Convert 
    //    void T.Method(object)
    // -> MessageHandler<T>
    internal static class MessageHandlerFuncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(MessageHandlerFuncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static MessageHandler<T> Build<T>(MethodInfo method)
            where T : class
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                typeof(T), method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (MessageHandler<T>)ret;
        }

        private static MessageHandler<TTarget> BuildHelper<TTarget, TParam>(MethodInfo method)
            where TTarget : class
        {
            var func = (Action<TTarget, TParam>)Delegate.CreateDelegate(
                typeof(Action<TTarget, TParam>), method);

            return (target, param) =>
            {
                func(target, (TParam)param);
            };
        }
    }

    // Convert 
    //    Task T.Method(object)
    // -> MessageAsyncHandler<T>
    internal static class MessageHandlerAsyncBuilder
    {
        private static readonly MethodInfo _buildHelperMethodInfo =
            typeof(MessageHandlerAsyncBuilder).GetMethod(
                "BuildHelper", BindingFlags.Static | BindingFlags.NonPublic);

        public static MessageAsyncHandler<T> Build<T>(MethodInfo method)
            where T : class
        {
            var constructedHelper = _buildHelperMethodInfo.MakeGenericMethod(
                typeof(T), method.GetParameters()[0].ParameterType);

            var ret = constructedHelper.Invoke(null, new object[] { method });
            return (MessageAsyncHandler<T>)ret;
        }

        private static MessageAsyncHandler<TTarget> BuildHelper<TTarget, TParam>(MethodInfo method)
            where TTarget : class
        {
            var func = (Func<TTarget, TParam, Task>)Delegate.CreateDelegate(
                typeof(Func<TTarget, TParam, Task>), method);

            return (target, param) =>
            {
                return func(target, (TParam)param);
            };
        }
    }
}
