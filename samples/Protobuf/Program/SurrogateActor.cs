﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Protobuf.Interface;

namespace Protobuf.Program
{
    public class SurrogateActor : InterfacedActor, ISurrogate
    {
        Task<ActorPath> ISurrogate.GetPath(ActorPath path)
        {
            return Task.FromResult(path);
        }

        Task<Address> ISurrogate.GetAddress(Address address)
        {
            return Task.FromResult(address);
        }

        Task<ISurrogate> ISurrogate.GetSelf()
        {
            Console.WriteLine("GetSelf return {0}", Self.Path);
            return Task.FromResult<ISurrogate>(Self.Cast<SurrogateRef>()); // BE CAREFUL: DON'T DO just "this"
        }
    }
}
