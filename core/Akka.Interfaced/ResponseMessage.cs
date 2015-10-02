using System;

namespace Akka.Interfaced
{
    public class ResponseMessage
    {
        public int RequestId;
        public IValueGetable ReturnPayload;
        public Exception Exception;
    }
}
