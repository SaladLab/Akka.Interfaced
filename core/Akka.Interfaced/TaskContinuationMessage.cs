using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    internal class TaskContinuationMessage
    {
        public MessageHandleContext Context;
        public SendOrPostCallback CallbackAction;
        public object CallbackState;
    }
}
