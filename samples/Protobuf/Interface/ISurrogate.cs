using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using ProtoBuf;

namespace Protobuf.Interface
{
    public interface ISurrogate : IInterfacedActor
    {
        Task<ActorPath> GetPath(ActorPath path);
        Task<Address> GetAddress(Address address);
        Task<ActorRefBase> GetSelf();
    }
}
