namespace Akka.Interfaced
{
    public class RequestMessage
    {
        public int RequestId;
        public IAsyncInvokable InvokePayload;
    }
}
