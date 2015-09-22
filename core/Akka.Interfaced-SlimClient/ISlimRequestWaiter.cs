using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface ISlimRequestWaiter
    {
        Task SendRequestAndWait(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout);
        Task<T> SendRequestAndReceive<T>(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout);
    }
}
