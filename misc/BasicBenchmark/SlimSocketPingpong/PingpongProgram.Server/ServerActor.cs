#define USE_EXTENDED_INTERFACE

using Akka.Interfaced;
using PingpongInterface;

namespace PingpongProgram.Server
{
#if USE_EXTENDED_INTERFACE
    public class ServerActor : InterfacedActor<ServerActor>, IExtendedInterface<IServer>
    {
        [ExtendedHandler]
        private int Echo(int value)
        {
            return value;
        }
    }
#else
    public class ServerActor : InterfacedActor<ServerActor>, IServer
    {
        Task<int> IServer.Echo(int value)
        {
            return Task.FromResult(value);
        }
    }
#endif
}
