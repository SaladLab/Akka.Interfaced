using System;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class TagOverridableAttribute : Attribute
    {
        public string Name { get; }

        public TagOverridableAttribute(string name)
        {
            Name = name;
        }
    }
}
