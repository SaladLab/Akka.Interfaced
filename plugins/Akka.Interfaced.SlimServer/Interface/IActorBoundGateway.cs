using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public interface IActorBoundGateway : IInterfacedActor
    {
        Task<string> OpenChannel(IActorRef actor, TaggedType[] types, ActorBindingFlags bindingFlags = 0);
    }
}
