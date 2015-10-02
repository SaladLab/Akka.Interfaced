using System;

namespace Akka.Interfaced
{
    // Method invoke response message
    public class ResponseMessage
    {
        public int RequestId;
        public IValueGetable ReturnPayload;
        public Exception Exception;
    }
}
