using System;

namespace Akka.Interfaced
{
    public class SlimReplyMessage
    {
        public int RequestId;
        public object Result;
        public Exception Exception;
    }
}
