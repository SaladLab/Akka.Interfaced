using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public interface IRequestWaiter
    {
        void SendRequest(IRequestTarget target, RequestMessage requestMessage);
        Task SendRequestAndWait(IRequestTarget target, RequestMessage requestMessage, TimeSpan? timeout);
        Task<TReturn> SendRequestAndReceive<TReturn>(IRequestTarget target, RequestMessage requestMessage, TimeSpan? timeout);
    }
}
