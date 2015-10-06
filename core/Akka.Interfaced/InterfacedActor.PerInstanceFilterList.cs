using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public class InterfacedActorPerInstanceFilterList
    {
        private IFilter[] _filters;

        public InterfacedActorPerInstanceFilterList()
        {
        }

        public InterfacedActorPerInstanceFilterList(object self, List<Func<object, IFilter>> creators)
        {
            Create(self, creators);
        }

        public void Create(object self, List<Func<object, IFilter>> creators)
        {
            _filters = new IFilter[creators.Count];
            for (var i = 0; i < creators.Count; i++)
            {
                _filters[i] = creators[i](self);
            }
        }

        public IFilter Get(int index)
        {
            return _filters[index];
        }
    }
}
