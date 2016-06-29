using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public interface IActorBoundGateway : IInterfacedActor
    {
        Task<InterfacedActorRef> OpenChannel(InterfacedActorRef actor, ActorBindingFlags bindingFlags = 0);
        Task<BoundActorTarget> OpenChannel(IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags = 0);
    }
}
