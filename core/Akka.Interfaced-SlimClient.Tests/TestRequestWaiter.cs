using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Akka.Interfaced.SlimClient.Tests
{
    public class TestRequestWaiter : IRequestWaiter
    {
        public List<RequestMessage> Requests = new List<RequestMessage>();
        public Queue<IValueGetable> Responses = new Queue<IValueGetable>();

        public void SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            Requests.Add(requestMessage);
        }

        public Task SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            Requests.Add(requestMessage);
            return Task.FromResult(true);
        }

        public Task<TReturn> SendRequestAndReceive<TReturn>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            Requests.Add(requestMessage);
            return Task.FromResult((TReturn)(Responses.Dequeue().Value));
        }
    }
}
