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

namespace SlimUnity.Program.Server
{
    class ActorSession : UntypedActor
    {
        private IActorRef _target1;
        private IActorRef _target2;
        private IActorRef _target3;
        private IActorRef _self;
        private ActorService _service;
        private TcpConnection _connection;
        private Socket _socket;

        public ActorSession(ActorService service, TcpConnection connection, Socket socket)
        {
            _target1 = Context.ActorSelection("/user/counter").ResolveOne(TimeSpan.Zero).Result;
            _target2 = Context.ActorSelection("/user/calculator").ResolveOne(TimeSpan.Zero).Result;
            _target3 = Context.ActorSelection("/user/pedantic").ResolveOne(TimeSpan.Zero).Result;
            _service = service;
            _connection = connection;
            _socket = socket;
        }

        protected override void PreStart()
        {
            base.PreStart();

            _self = Self;

            _service.OnSessionCreated(this);

            _connection.Closed += OnConnectionClose;
            _connection.Received += OnConnectionReceive;
            _connection.Settings = new TcpConnectionSettings
            {
                PacketSerializer = new PacketSerializer(
                    new PacketSerializerBase.Data(
                        new ProtoBufMessageSerializer(ProtobufSerializer.CreateTypeModel()),
                        new TypeAliasTable()))
            };

            _connection.Open(_socket);
        }

        protected override void PostStop()
        {
            base.PostStop();

            _service.OnSessionDestroyed(this);
        }

        protected override void OnReceive(object message)
        {
            var response = message as ResponseMessage;
            if (response != null)
            {
                var targetId = 0;
                if (Sender == _target1)
                    targetId = 1;
                else if (Sender == _target2)
                    targetId = 2;
                else if (Sender == _target3)
                    targetId = 3;

                if (targetId != 0)
                {
                    _connection.Send(new Packet
                    {
                        Type = PacketType.Response,
                        ActorId = 1,
                        RequestId = response.RequestId,
                        Message = response.ReturnPayload,
                        Exception = response.Exception
                    });
                }
            }
        }

        protected void OnConnectionClose(TcpConnection connection, int reason)
        {
            _self.Tell(InterfacedPoisonPill.Instance);
        }

        protected void OnConnectionReceive(TcpConnection connection, object packet)
        {
            var p = packet as Packet;

            if (p == null || p.Message == null)
            {
                return;
            }

            IActorRef target = null;
            if (p.ActorId == 1)
                target = _target1;
            if (p.ActorId == 2)
                target = _target2;
            if (p.ActorId == 3)
                target = _target3;

            if (target != null)
            {
                target.Tell(new RequestMessage
                {
                    RequestId = p.RequestId,
                    InvokePayload = (IAsyncInvokable)p.Message
                }, _self);
            }
        }
    }
}
