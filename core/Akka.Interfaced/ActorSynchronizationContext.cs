using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    internal class MessageHandleContext
    {
        public IActorRef Self { get; set; }
        public IActorRef Sender { get; set; }
    }

    internal class ActorSynchronizationContext : SynchronizationContext
    {
        private readonly MessageHandleContext _context;

        public ActorSynchronizationContext(MessageHandleContext context)
        {
            _context = context;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new ActorSynchronizationContext(_context);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _context.Self.Tell(
                new TaskContinuationMessage { Context = _context, CallbackAction = d, CallbackState = state },
                _context.Sender);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotImplementedException("Send");
        }
    }
}
