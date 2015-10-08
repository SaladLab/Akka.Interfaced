using System;
using Akka.Interfaced;

class SlimRequestWaiter : IRequestWaiter
{
    public Communicator Communicator { get; internal set; }

    public void SendRequest(IActorRef target, RequestMessage requestMessage)
    {
        Communicator.SendRequest(target, requestMessage);
    }

    Task IRequestWaiter.SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
    {
        return Communicator.SendRequestAndWait(target, requestMessage, timeout);
    }

    Task<T> IRequestWaiter.SendRequestAndReceive<T>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
    {
        return Communicator.SendRequestAndReceive<T>(target, requestMessage, timeout);
    }
}
