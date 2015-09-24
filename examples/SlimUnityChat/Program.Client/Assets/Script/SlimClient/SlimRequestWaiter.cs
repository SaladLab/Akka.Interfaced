using System;
using Akka.Interfaced;

class SlimRequestWaiter : ISlimRequestWaiter
{
    public Communicator Communicator { get; internal set; }

    void ISlimRequestWaiter.SendRequest(ISlimActorRef target, SlimRequestMessage requestMessage)
    {
        Communicator.SendRequest(target, requestMessage);
    }

    Task ISlimRequestWaiter.SendRequestAndWait(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
    {
        return Communicator.SendRequestAndWait(target, requestMessage, timeout);
    }

    Task<T> ISlimRequestWaiter.SendRequestAndReceive<T>(ISlimActorRef target, SlimRequestMessage requestMessage, TimeSpan? timeout)
    {
        return Communicator.SendRequestAndReceive<T>(target, requestMessage, timeout);
    }
}
