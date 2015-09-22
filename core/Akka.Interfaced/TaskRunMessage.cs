using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    internal class TaskRunMessage
    {
        public Func<Task> Function;
        public bool IsReentrant;
    }
}
