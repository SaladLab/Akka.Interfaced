using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IFilter
    {
        int Order { get; }
    }

    public class PreHandleFilterContext
    {
        public object Actor;
        public RequestMessage Request;
        public ResponseMessage Response;
    }

    public interface IPreHandleFilter : IFilter
    {
        void OnPreHandle(PreHandleFilterContext context);
    }

    public interface IPreHandleAsyncFilter : IFilter
    {
        Task OnPreHandle(PreHandleFilterContext context);
    }

    public class PostHandleFilterContext
    {
        public object Actor;
        public RequestMessage Request;
        public ResponseMessage Response;
    }

    public interface IPostHandleFilter : IFilter
    {
        void OnPostHandle(PostHandleFilterContext context);
    }

    public interface IPostHandleAsyncFilter : IFilter
    {
        Task OnPostHandle(PostHandleFilterContext context);
    }

    public interface IFilterFactory
    {
        IFilter CreateInstance(Type actorType, MethodInfo method);
    }

    // TODO: Which one is better to support lifetime scope of filter ? 

    /*
    public interface IFilterPerInstanceFactory
    {
        IFilter CreateInstance(Type actorType, object self);
    }

    public interface IFilterPerInvokeFactory
    {
        IFilter CreateInstance(Type actorType, object self, RequestMessage request);
    }

    public enum FilterLifetimeScope
    {
        PerClass = 0,
        PerInstance = 1,
        PerInvoke = 2
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FilterLifetimeScopeAttribute : Attribute
    {
        public FilterLifetimeScope Scope { get;}

        public FilterLifetimeScopeAttribute(FilterLifetimeScope scope)
        {
            Scope = scope;
        }
    }
    */
}
