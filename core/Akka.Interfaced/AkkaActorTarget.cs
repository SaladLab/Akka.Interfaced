using Akka.Actor;

namespace Akka.Interfaced
{
    public class AkkaActorTarget : IRequestTarget
    {
        public IActorRef Actor { get; set; }

        public AkkaActorTarget()
        {
        }

        public AkkaActorTarget(IActorRef actor)
        {
            Actor = actor;
        }

        public IRequestWaiter DefaultRequestWaiter => AkkaAskRequestWaiter.Instance;
    }
}
