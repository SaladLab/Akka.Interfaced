using Akka.Actor;

namespace AkkaPingpong
{
    public class ServerMessages
    {
        public class EchoRequest
        {
            public int Value;
        }

        public class EchoResponse
        {
            public int Value;
        }
    }

    public class ServerActor : ReceiveActor
    {
        public ServerActor()
        {
            Receive<ServerMessages.EchoRequest>(m =>
            {
                Sender.Tell(new ServerMessages.EchoResponse { Value = m.Value });
            });
        }
    }
}
