using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessageTableForInterfacedActorAttribute : Attribute
    {
        public Type Type { get; private set; }

        public MessageTableForInterfacedActorAttribute(Type type)
        {
            Type = type;
        }
    }
}
