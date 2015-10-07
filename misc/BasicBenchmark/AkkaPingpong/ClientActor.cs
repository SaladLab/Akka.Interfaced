using Akka.Actor;

namespace AkkaPingpong
{
    public class ClientMessages
    {
        public class StartRequest
        {
            public int Count;
        }

        public class StartResponse
        {
        }
    }

    public class ClientActor : ReceiveActor
    {
        private IActorRef _driver;
        private IActorRef _server;
        private int _count;

        public ClientActor(IActorRef server)
        {
            _server = server;

            Receive<ClientMessages.StartRequest>(m =>
            {
                _driver = Sender;
                _count = m.Count;
                _server.Tell(new ServerMessages.EchoRequest { Value = 0 });
            });
            Receive<ServerMessages.EchoResponse>(m =>
            {
                if (m.Value + 1 >= _count)
                    _driver.Tell(new ClientMessages.StartResponse());
                else
                    Sender.Tell(new ServerMessages.EchoRequest { Value = m.Value + 1 });
            });
        }
    }
}
