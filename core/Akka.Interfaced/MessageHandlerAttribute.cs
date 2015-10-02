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
}
