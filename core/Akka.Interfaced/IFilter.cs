using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IFilter
    {
        int Order { get; }
    }

    // Pre-Request Filter

    public class PreRequestFilterContext
    {
        public object Actor;
        public RequestMessage Request;
        public ResponseMessage Response;
        public Exception Exception;
        public bool Handled => Response != null || Exception != null;
    }

    public interface IPreRequestFilter : IFilter
    {
        void OnPreRequest(PreRequestFilterContext context);
    }

    public interface IPreRequestAsyncFilter : IFilter
    {
        Task OnPreRequestAsync(PreRequestFilterContext context);
    }

    // Post-Request Filter

    public class PostRequestFilterContext
    {
        public object Actor;
        public RequestMessage Request;
        public ResponseMessage Response;
        public Exception Exception;
        public bool Intercepted;
    }

    public interface IPostRequestFilter : IFilter
    {
        void OnPostRequest(PostRequestFilterContext context);
    }

    public interface IPostRequestAsyncFilter : IFilter
    {
        Task OnPostRequestAsync(PostRequestFilterContext context);
    }

    // Pre-Notification Filter

    public class PreNotificationFilterContext
    {
        public object Actor;
        public NotificationMessage Notification;
        public bool Handled;
    }

    public interface IPreNotificationFilter : IFilter
    {
        void OnPreNotification(PreNotificationFilterContext context);
    }

    public interface IPreNotificationAsyncFilter : IFilter
    {
        Task OnPreNotificationAsync(PreNotificationFilterContext context);
    }

    // Post-Notification Filter

    public class PostNotificationFilterContext
    {
        public object Actor;
        public NotificationMessage Notification;
        public bool Intercepted;
    }

    public interface IPostNotificationFilter : IFilter
    {
        void OnPostNotification(PostNotificationFilterContext context);
    }

    public interface IPostNotificationAsyncFilter : IFilter
    {
        Task OnPostNotificationAsync(PostNotificationFilterContext context);
    }

    // Pre-Message Filter

    public class PreMessageFilterContext
    {
        public object Actor;
        public object Message;
        public bool Handled;
    }

    public interface IPreMessageFilter : IFilter
    {
        void OnPreMessage(PreMessageFilterContext context);
    }

    public interface IPreMessageAsyncFilter : IFilter
    {
        Task OnPreMessageAsync(PreMessageFilterContext context);
    }

    // Post-Message Filter

    public class PostMessageFilterContext
    {
        public object Actor;
        public object Message;
        public bool Intercepted;
    }

    public interface IPostMessageFilter : IFilter
    {
        void OnPostMessage(PostMessageFilterContext context);
    }

    public interface IPostMessageAsyncFilter : IFilter
    {
        Task OnPostMessageAsync(PostMessageFilterContext context);
    }
}
