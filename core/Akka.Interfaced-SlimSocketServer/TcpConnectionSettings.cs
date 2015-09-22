using Akka.Interfaced.SlimSocketBase;

namespace Akka.Interfaced.SlimSocketServer
{
    public class TcpConnectionSettings
    {
        public bool SocketNoDelay { get; set; }

        public int ReceiveBufferSize { get; set; }
        public int ReceiveBufferMaxSize { get; set; }
        public int SendBufferSize { get; set; }
        public int SendBufferMaxSize { get; set; }

        public IPacketSerializer PacketSerializer { get; set; }

        // TODO: Buffer Allocator
        // TODO: Stat Monitor

        public TcpConnectionSettings()
        {
            SocketNoDelay = false;

            ReceiveBufferSize = 1024;
            ReceiveBufferMaxSize = 1048576;
            SendBufferSize = 1024;
            SendBufferMaxSize = 1048576;
        }
    }
}
