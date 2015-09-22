using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;
using System.Collections.Generic;

namespace SlimUnityChat.Interface
{
    public interface IRoom : IInterfacedActor
    {
        Task<RoomInfo> Enter(string userId, IRoomObserver observer);
        Task Exit(string userId);
    }
}
