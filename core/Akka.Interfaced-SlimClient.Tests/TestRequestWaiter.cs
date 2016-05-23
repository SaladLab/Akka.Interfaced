using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Akka.Interfaced.SlimClient.Tests
{
    public class TestRequestWaiter : IRequestWaiter
    {
        public List<RequestMessage> Requests = new List<RequestMessage>();

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
            return Task.FromResult(default(TReturn));
        }
    }
}
