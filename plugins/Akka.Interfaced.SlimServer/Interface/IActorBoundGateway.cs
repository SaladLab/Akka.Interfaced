using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced.SlimServer
{
    public interface IActorBoundGateway : IInterfacedActor
    {
        Task<string> OpenChannel(IActorRef actor, object channelClosedNotification, params TaggedType[] types);
    }
}
