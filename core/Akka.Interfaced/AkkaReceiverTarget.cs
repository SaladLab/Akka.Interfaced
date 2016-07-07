using Akka.Actor;

namespace Akka.Interfaced
{
    public class AkkaReceiverTarget : IRequestTarget
    {
        public ICanTell Receiver { get; }

        public AkkaReceiverTarget()
        {
        }

        public AkkaReceiverTarget(ICanTell receiver)
        {
            Receiver = receiver;
        }

        public IRequestWaiter DefaultRequestWaiter => AkkaAskRequestWaiter.Instance;
    }
}
