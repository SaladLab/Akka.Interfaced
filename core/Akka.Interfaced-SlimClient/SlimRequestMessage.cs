using System;

namespace Akka.Interfaced
{
    public class SlimRequestMessage
    {
        public int RequestId;
        public object InvokePayload;
    }
}
