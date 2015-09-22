using System;

namespace Akka.Interfaced
{
    public class ReplyMessage
    {
        public int RequestId;
        public IValueGetable Result;
        public Exception Exception;
    }
}
