using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using PingpongInterface;

namespace PingpongProgram
{
    public class ClientActor : InterfacedActor<ClientActor>, IExtendedInterface<IClient>
    {
        private ServerRef _server;

        public ClientActor(IActorRef server)
        {
            _server = new ServerRef(server, this, null);
        }

        [ExtendedHandler]
        private async Task Start(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ret = await _server.Echo(i);
                if (i != ret)
                    throw new InvalidOperationException($"Wrong response (Expected:{i}, Actual:{ret})");
            }
        }
    }
}
