using System;
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
            var func = (Func<TTarget, TParam, Task>)Delegate.CreateDelegate(
                typeof(Func<TTarget, TParam, Task>), method);

            MessageHandler<TTarget> ret = (target, param) =>
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

            MessageHandler<TTarget> ret = (target, param) =>
            {
                func(target, (TParam)param);
                return null;
            };
            return ret;
        }
    }
}
