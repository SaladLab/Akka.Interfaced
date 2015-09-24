using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using SlimUnityChat.Interface;

namespace SlimUnityChat.Program.Server
{
    public class RoomDirectoryActor : InterfacedActor<RoomDirectoryActor>, IRoomDirectory
    {
        private ClusterNodeContext _clusterContext;
        private List<RoomDirectoryWorkerRef> _workers;
        private int _lastWorkIndex = -1;
        private Dictionary<string, Tuple<RoomDirectoryWorkerRef, IRoom>> _roomTable;

        public RoomDirectoryActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IRoomDirectory) },
                Self);
            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.WatchActor { Type = typeof(IRoomDirectoryWorker) },
                Self);

            _workers = new List<RoomDirectoryWorkerRef>();
            _roomTable = new Dictionary<string, Tuple<RoomDirectoryWorkerRef, IRoom>>();
        }

        protected override void OnReceiveUnhandled(object message)
        {
            var actorUp = message as ActorDiscoveryMessage.ActorUp;
            if (actorUp != null)
            {
                _workers.Add(new RoomDirectoryWorkerRef(actorUp.Actor, this, null));
                Console.WriteLine("<><> RoomDirectoryActor GOT Worker {0} <><>", actorUp.Actor.Path);
                return;
            }

            var actorDown = message as ActorDiscoveryMessage.ActorDown;
            if (actorDown != null)
            {
                _workers.RemoveAll(w => w.Actor == actorDown.Actor);
                Console.WriteLine("<><> RoomDirectoryWorkerActor LOST RoomDirectory {0} <><>", actorDown.Actor.Path);
                return;
            }

            var shutdownMessage = message as ShutdownMessage;
            if (shutdownMessage != null)
            {
                Context.Stop(Self);
                return;
            }

            base.OnReceiveUnhandled(message);
        }

        async Task<IRoom> IRoomDirectory.GetOrCreateRoom(string name)
        {
            Tuple<RoomDirectoryWorkerRef, IRoom> room = null;
            if (_roomTable.TryGetValue(name, out room))
                return room.Item2;

            if (_workers.Count == 0)
                return null;

            // pick a worker for creating RoomActor by round-robin fashion.

            _lastWorkIndex = (_lastWorkIndex + 1) % _workers.Count;
            var worker = _workers[_lastWorkIndex];

            try
            {
                room = Tuple.Create(worker, await worker.CreateRoom(name));
            }
            catch (Exception e)
            {
                // TODO: Write down exception log
                Console.WriteLine(e);
            }

            if (room == null)
                return null;

            _roomTable.Add(name, room);
            return room.Item2;
        }

        Task IRoomDirectory.RemoveRoom(string name)
        {
            Tuple<RoomDirectoryWorkerRef, IRoom> room = null;
            if (_roomTable.TryGetValue(name, out room) == false)
                return Task.FromResult(0);

            _roomTable.Remove(name);
            room.Item1.WithNoReply().RemoveRoom(name);

            return Task.FromResult(true);
        }

        Task<List<string>> IRoomDirectory.GetRoomList()
        {
            return Task.FromResult(_roomTable.Keys.ToList());
        }
    }
}
