using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;

namespace SlimUnityChat.Program.Server
{
    public class ChatBotCommanderMessage
    {
        public class Start
        {
        }
    }

    public class ChatBotCommanderActor : ReceiveActor
    {
        private ILog _logger = LogManager.GetLogger("ChatBotCommander");
        private ClusterNodeContext _clusterContext;
        private HashSet<IActorRef> _botSet = new HashSet<IActorRef>();
        private bool _isStopped;

        public ChatBotCommanderActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            Receive<ChatBotCommanderMessage.Start>(m => Handle(m));
            Receive<ShutdownMessage>(m => Handle(m));
            Receive<Terminated>(m => Handle(m));
        }

        private void Handle(ChatBotCommanderMessage.Start m)
        {
            if (_clusterContext.UserDirectory == null ||
                _clusterContext.RoomDirectory == null)
            {
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(1), Self, m, Self);
                return;
            }

            var chatBot = Context.ActorOf(Props.Create<ChatBotActor>(_clusterContext));
            chatBot.Tell(new ChatBotMessage.Start { UserId = "bot1", RoomName = "#bot1" });
            Context.Watch(chatBot);
            _botSet.Add(chatBot);
        }

        private void Handle(ShutdownMessage m)
        {
            if (_isStopped)
                return;

            _logger.Info("Stop");
            _isStopped = true;

            // stop all running client sessions

            if (_botSet.Count > 0)
            {
                Context.ActorSelection("*").Tell(InterfacedPoisonPill.Instance);
            }
            else
            {
                Context.Stop(Self);
            }
        }

        private void Handle(Terminated m)
        {
            _botSet.Remove(m.ActorRef);

            if (_isStopped && _botSet.Count == 0)
                Context.Stop(Self);
        }
    }
}