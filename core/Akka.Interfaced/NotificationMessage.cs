namespace Akka.Interfaced
{
    // Observer event notification message
    public class NotificationMessage
    {
        public int ObserverId;
        public IInvokable InvokePayload;
    }
}
