using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;

namespace Protobuf.Interface
{
    public interface ISurrogate : IInterfacedActor
    {
        Task<ActorPath> GetPath(ActorPath path);
        Task<Address> GetAddress(Address address);
        Task<ISurrogate> GetSelf();
    }
}
