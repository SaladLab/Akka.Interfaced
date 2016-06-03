using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoFilter
    {
        private ActorSystem _system;

        public DemoFilter(ActorSystem system, string[] args)
        {
            _system = system;
        }
    }
}
