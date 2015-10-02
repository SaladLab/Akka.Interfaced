using System;
using Akka.Actor;
using Akka.Event;

namespace Akka.Interfaced
{
    public class DeadRequestProcessingActor : UntypedActor
    {
        protected override void PreStart()
        {
            Context.System.EventStream.Subscribe(Self, typeof(DeadLetter));
        }

        protected override void PostStop()
        {
            Context.System.EventStream.Unsubscribe(Self, typeof(DeadLetter));
        }

        protected override void OnReceive(object message)
        {
            var deadLetter = message as DeadLetter;
            if (deadLetter != null)
            {
                var request = deadLetter.Message as RequestMessage;
                if (request != null && request.RequestId != 0)
                {
                    var response = new ResponseMessage
                    {
                        RequestId = request.RequestId,
                        Exception = new InvalidOperationException("Actor not found")
                    };
                    deadLetter.Sender.Tell(response);
                }
            }
        }

        public static void Install(ActorSystem system)
        {
            system.ActorOf<DeadRequestProcessingActor>("DeadRequestProcessingActor");
        }
    }
}
