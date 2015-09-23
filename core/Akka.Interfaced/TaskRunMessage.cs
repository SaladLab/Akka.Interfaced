using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    internal class TaskRunMessage
    {
        public Func<Task> Function;
        public bool IsReentrant;
    }
}
