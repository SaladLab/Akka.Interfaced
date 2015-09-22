using System;
using Akka.Interfaced;

namespace SlimUnityChat.Interface
{
    public interface IUserEventObserver : IInterfacedObserver
    {
        void Whisper(ChatItem chatItem);
        void Invite(string invitorUserId, string roomName);
    }
}
