using System;
using System.Net;
using System.Net.Sockets;
using Akka.Actor;
using Akka.Interfaced.SlimSocketServer;

namespace PingpongProgram.ServerLib
{
    public class ClientGatewayMessage
    {
        public class Start
        {
            public IPEndPoint ServiceEndPoint;
        }

        public class Accept
        {
            public Socket Socket;
        }
    }

    public class ClientGateway : ReceiveActor
    {
        private TcpAcceptor _tcpAcceptor;

        public ClientGateway()
        {
            Receive<ClientGatewayMessage.Start>(m => Handle(m));
            Receive<ClientGatewayMessage.Accept>(m => Handle(m));
        }

        private void Handle(ClientGatewayMessage.Start m)
        {
            try
            {
                var self = Self;
                _tcpAcceptor = new TcpAcceptor();
                _tcpAcceptor.Accepted += (sender, socket) =>
                {
                    self.Tell(new ClientGatewayMessage.Accept { Socket = socket }, self);
                    return TcpAcceptor.AcceptResult.Accept;
                };
                _tcpAcceptor.Listen(m.ServiceEndPoint);
                Console.WriteLine($"Start Listen {m.ServiceEndPoint}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Handle(ClientGatewayMessage.Accept m)
        {
            Context.ActorOf(Props.Create<ClientSession>(m.Socket));
        }
    }
}
