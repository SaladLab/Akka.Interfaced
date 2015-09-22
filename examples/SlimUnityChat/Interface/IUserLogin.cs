using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;

namespace SlimUnityChat.Interface
{
    public interface IUserLogin : IInterfacedActor
    {
        Task<int> Login(string id, string password, int observerId);
    }
}
