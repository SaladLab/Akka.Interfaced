using Akka.Interfaced;
using PingpongInterface;

namespace PingpongProgram
{
    public class ServerActor : InterfacedActor<ServerActor>, IExtendedInterface<IServer>
    {
        [ExtendedHandler]
        private int Echo(int value)
        {
            return value;
        }
    }
}
