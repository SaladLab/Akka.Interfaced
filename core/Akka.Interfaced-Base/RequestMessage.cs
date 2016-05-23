namespace Akka.Interfaced
{
    // Method invoke request message
    public class RequestMessage
    {
        public int RequestId;
        public IInterfacedPayload InvokePayload;
    }
}
