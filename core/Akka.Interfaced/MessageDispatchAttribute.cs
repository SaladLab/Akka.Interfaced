using System;

namespace Akka.Interfaced
{
    public class MessageDispatchAttribute : Attribute
    {
        public Type Type;

        public MessageDispatchAttribute()
        {
            Type = null;
        }

        public MessageDispatchAttribute(Type type)
        {
            Type = type;
        }
    }
}
