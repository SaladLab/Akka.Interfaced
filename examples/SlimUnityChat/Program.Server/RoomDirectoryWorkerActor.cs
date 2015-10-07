using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using SlimUnityChat.Interface;
using Akka.Actor;
using System.Collections.Generic;
using Common.Logging;

namespace SlimUnityChat.Program.Server
{
    public class RoomDirectoryWorkerActor : InterfacedActor<RoomDirectoryWorkerActor>, IRoomDirectoryWorker
    {
        private ILog _logger = LogManager.GetLogger("RoomDirectoryWorker");
        private ClusterNodeContext _context;
        private RoomDirectoryRef _roomDirectory;
        private Dictionary<string, IRoom> _roomTable;
        private int _roomActorCount;
        private bool _isStopped;

        public RoomDirectoryWorkerActor(ClusterNodeContext context)
        {
            _context = context;

            _context.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IRoomDirectoryWorker) },
                Self);
            _context.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.WatchActor { Type = typeof(IRoomDirectory) },
                Self);

            _roomTable = new Dictionary<string, IRoom>();
        }

        protected override void OnReceiveUnhandled(object message)
        {
            var actorUp = message as ActorDiscoveryMessage.ActorUp;
            if (actorUp != null)
            {
                _roomDirectory = new RoomDirectoryRef(actorUp.Actor, this, null);
                Console.WriteLine("<><> RoomDirectoryWorkerActor GOT RoomDirectory {0} <><>", actorUp.Actor.Path);
                return;
            }

            var actorDown = message as ActorDiscoveryMessage.ActorDown;
            if (actorDown != null)
            {
                if (_roomDirectory != null && _roomDirectory.Actor == actorDown.Actor)
                {
                    _roomDirectory = null;
                    Console.WriteLine("<><> RoomDirectoryWorkerActor LOST RoomDirectory {0} <><>", actorDown.Actor.Path);
                }
                return;
            }

            var shutdownMessage = message as ShutdownMessage;
            if (shutdownMessage != null)
            {
                Handle(shutdownMessage);
                return;
            }

            var terminated = message as Terminated;
            if (terminated != null)
            {
                Handle(terminated);
                return;
            }

            base.OnReceiveUnhandled(message);
        }

        private void Handle(ShutdownMessage m)
        {
            if (_isStopped)
                return;

            _logger.Info("Stop");
            _isStopped = true;

            // stop all running client sessions

            if (_roomActorCount > 0)
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
            _roomActorCount -= 1;
            if (_isStopped && _roomActorCount == 0)
                Context.Stop(Self);
        }

        Task<IRoom> IRoomDirectoryWorker.CreateRoom(string name)
        {
            // create room actor

            IActorRef roomActor = null;
            try
            {
                roomActor = Context.ActorOf(Props.Create<RoomActor>(_context, name));
                Context.Watch(roomActor);
                _roomActorCount += 1;
            }
            catch (Exception)
            {
                return Task.FromResult((IRoom)null);
            }

            // register it at local directory and return

            var room = new RoomRef(roomActor);
            _roomTable.Add(name, room);
            return Task.FromResult((IRoom)room);
        }

        Task IRoomDirectoryWorker.RemoveRoom(string name)
        {
            IRoom room;
            if (_roomTable.TryGetValue(name, out room) == false)
                return Task.FromResult(0);

            ((RoomRef)room).Actor.Tell(InterfacedPoisonPill.Instance);
            _roomTable.Remove(name);
            return Task.FromResult(0);
        }
    }
}
