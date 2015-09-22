using System;

namespace Akka.Interfaced.SlimSocketBase
{
    public enum PacketType
    {
        Notification = 1, 
        Request = 2,
        Reply = 3,
    }

    public class Packet
    {
        public PacketType Type;
        public int ActorId;
        public int RequestId;
        public object Message;
        public Exception Exception;
    }
}
