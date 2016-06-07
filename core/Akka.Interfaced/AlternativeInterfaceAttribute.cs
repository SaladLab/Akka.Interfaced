using System;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class AlternativeInterfaceAttribute : Attribute
    {
        public Type Type;

        public AlternativeInterfaceAttribute(Type interfaceType)
        {
            Type = interfaceType;
        }
    }
}
