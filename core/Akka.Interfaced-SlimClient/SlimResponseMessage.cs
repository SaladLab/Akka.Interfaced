using System;

namespace Akka.Interfaced
{
    public class SlimResponseMessage
    {
        public int RequestId;
        public IValueGetable ReturnPayload;
        public Exception Exception;
    }
}
