using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Interfaced;

namespace SlimUnityChat.Interface
{
    // Any user who is in a room
    [TagOverridable("senderUserId")]
    public interface IOccupant : IInterfacedActor
    {
        Task Say(string msg, string senderUserId = null);
        Task<List<ChatItem>> GetHistory();
        Task Invite(string targetUserId, string senderUserId = null);
    }
}
