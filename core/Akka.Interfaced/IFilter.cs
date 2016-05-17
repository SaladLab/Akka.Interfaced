using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IFilter
    {
        int Order { get; }
    }

    // Pre-Handle Filter

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

    // Post-Handle Filter

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
