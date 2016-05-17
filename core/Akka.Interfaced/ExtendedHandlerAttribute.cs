using System;

namespace Akka.Interfaced
{
    public class ExtendedHandlerAttribute : Attribute
    {
        public Type Type;
        public string Method;

        public ExtendedHandlerAttribute()
        {
        }

        public ExtendedHandlerAttribute(Type interfaceType, string method = null)
        {
            Type = interfaceType;
            Method = method;
        }
    }
}
