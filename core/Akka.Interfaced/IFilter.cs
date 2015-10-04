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
        Task OnPreHandleAsync(PreHandleFilterContext context);
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
        Task OnPostHandleAsync(PostHandleFilterContext context);
    }
}
