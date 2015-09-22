using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public interface IRequestWaiter
    {
        Task<object> SendRequestAndReceive(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout);
    }
}
