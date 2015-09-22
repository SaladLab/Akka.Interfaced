using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ReentrantAttribute : Attribute
    {
    }
}
