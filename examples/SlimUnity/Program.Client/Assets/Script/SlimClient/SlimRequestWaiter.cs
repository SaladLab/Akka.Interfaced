using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using Akka.Interfaced;

class SlimRequestWaiter : ISlimRequestWaiter
{
    public Communicator Communicator { get; internal set; }

    Task ISlimRequestWaiter.SendRequestAndWait(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
    {
        return Communicator.SendRequestAndWait(target, requestMessage, timeout);
    }

    Task<T> ISlimRequestWaiter.SendRequestAndReceive<T>(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
    {
        return Communicator.SendRequestAndReceive<T>(target, requestMessage, timeout);
    }
}
