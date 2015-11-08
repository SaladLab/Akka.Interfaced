using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Interfaced;

namespace SlimHttp.Program.Client
{
    class SlimActorRef : IActorRef
    {
        public string Id { get; set; }
    }
}
