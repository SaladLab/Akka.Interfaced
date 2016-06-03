using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;

namespace Manual
{
    public class DemoLogFilter
    {
        private ActorSystem _system;

        public DemoLogFilter(ActorSystem system, string[] args)
        {
            _system = system;
        }
    }
}
