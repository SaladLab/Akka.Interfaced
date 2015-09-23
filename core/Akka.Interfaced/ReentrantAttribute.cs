using System;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ReentrantAttribute : Attribute
    {
    }
}
