using System.Threading;

namespace Akka.Interfaced
{
    internal class TaskContinuationMessage
    {
        public MessageHandleContext Context;
        public SendOrPostCallback CallbackAction;
        public object CallbackState;
    }
}
