using System;

namespace Akka.Interfaced
{
    public enum PayloadTableKind
    {
        Request = 1,
        Notification = 2,
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PayloadTableAttribute : Attribute
    {
        public Type Type { get; private set; }
        public PayloadTableKind Kind { get; private set; }

        public PayloadTableAttribute(Type type, PayloadTableKind kind)
        {
            Type = type;
            Kind = kind;
        }
    }
}
