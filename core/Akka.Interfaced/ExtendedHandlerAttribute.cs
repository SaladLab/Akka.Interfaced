using System;

namespace Akka.Interfaced
{
    public class ExtendedHandlerAttribute : Attribute
    {
        public Type Type;

        public ExtendedHandlerAttribute()
        {
            Type = null;
        }

        public ExtendedHandlerAttribute(Type interfaceType)
        {
            Type = interfaceType;
        }
    }
}
