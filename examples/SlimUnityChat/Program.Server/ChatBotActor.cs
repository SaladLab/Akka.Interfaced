using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.ProtobufSerializer;
using Akka.Interfaced.SlimSocketBase;
using Akka.Interfaced.SlimSocketServer;
using TypeAlias;
using SlimUnityChat.Interface;
using System.Reflection;
using Common.Logging;

namespace SlimUnityChat.Program.Server
{
    public static class ChatBotMessage
    {
        public class Start
        {
            public string UserId;
            public string RoomName;
        }
    }

    public class ChatBotActor : InterfacedActor<ChatBotActor>
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private string _userId;
        private UserRef _user;
        private OccupantRef _occupant;

        public ChatBotActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;
        }

        protected override void OnReceiveUnhandled(object message)
        {
            if (message is ChatBotMessage.Start)
            {
                RunTask(() => Handle((ChatBotMessage.Start)message), true);
            }
            else if (message is ClientSession.BindActorRequestMessage)
            {
                Handle((ClientSession.BindActorRequestMessage)message);   
            }
            else
            {
                base.OnReceiveUnhandled(message);
            }

            // Notice 처리는?
        }

        private async Task Handle(ChatBotMessage.Start m)
        {
            if (_user != null)
                throw new InvalidOperationException("Already started");

            _userId = m.UserId;

            // start login

            var userLoginActor = Context.ActorOf(Props.Create<UserLoginActor>(_clusterContext, Self, new IPEndPoint(IPAddress.Loopback, 0)));
            var userLogin = new UserLoginRef(userLoginActor, this, null);
            await userLogin.Login(_userId, "bot", 1);

            // enter chat

            await _user.EnterRoom("#bot", 2);

            // chat !

            for (int i=0; i<10000; i++)
            {
                await _occupant.Say(DateTime.Now.ToString(), _userId);
                await Task.Delay(1000);
            }
        }

        private void Handle(ClientSession.BindActorRequestMessage m)
        {
            if (m.InterfaceType == typeof(IUser))
            {
                _user = new UserRef(m.Actor);
                Sender.Tell(new ClientSession.BindActorResponseMessage());
                return;
            }

            if (m.InterfaceType == typeof(IOccupant))
            {
                _occupant = new OccupantRef(m.Actor);
                Sender.Tell(new ClientSession.BindActorResponseMessage());
                return;
            }

            // TODO: 에러 처리?
        }
    }
}