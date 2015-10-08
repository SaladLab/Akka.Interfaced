using System;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PayloadTableForInterfacedActorAttribute : Attribute
    {
        public Type Type { get; private set; }

        public PayloadTableForInterfacedActorAttribute(Type type)
        {
            Type = type;
        }
    }
}
