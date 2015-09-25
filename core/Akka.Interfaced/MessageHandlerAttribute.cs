using System;

namespace Akka.Interfaced
{
    public class MessageHandlerAttribute : Attribute
    {
        public Type Type;

        public MessageHandlerAttribute()
        {
            Type = null;
        }

        public MessageHandlerAttribute(Type messageType)
        {
            Type = messageType;
        }
    }

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
