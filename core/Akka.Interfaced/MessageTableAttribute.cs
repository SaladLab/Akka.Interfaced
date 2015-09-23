using System;

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
