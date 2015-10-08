using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IRequestWaiter
    {
        void SendRequest(IActorRef target, RequestMessage requestMessage);
        Task SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout);
        Task<TReturn> SendRequestAndReceive<TReturn>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout);
    }
}
