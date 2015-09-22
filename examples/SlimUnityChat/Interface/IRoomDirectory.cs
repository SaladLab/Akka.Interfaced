using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;

namespace SlimUnityChat.Interface
{
    public interface IRoomDirectory : IInterfacedActor
    {
        Task<IRoom> GetOrCreateRoom(string name);
        Task RemoveRoom(string name);
        Task<List<string>> GetRoomList();
    }

    public interface IRoomDirectoryWorker : IInterfacedActor
    {
        Task<IRoom> CreateRoom(string name);
        Task RemoveRoom(string name);
    }
}
