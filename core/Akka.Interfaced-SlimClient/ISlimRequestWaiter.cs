using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface ISlimRequestWaiter
    {
        void SendRequest(ISlimActorRef target, SlimRequestMessage requestMessage);
        Task SendRequestAndWait(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout);
        Task<TReturn> SendRequestAndReceive<TReturn>(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout);
    }
}
