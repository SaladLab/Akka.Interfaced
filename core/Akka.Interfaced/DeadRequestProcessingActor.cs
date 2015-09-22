using Akka.Actor;
using Akka.Event;
using System;

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
                var requestMessage = deadLetter.Message as RequestMessage;
                if (requestMessage != null && requestMessage.RequestId != 0)
                {
                    var replyMessage = new ReplyMessage
                    {
                        RequestId = requestMessage.RequestId,
                        Exception = new InvalidOperationException("Actor not found")
                    };
                    deadLetter.Sender.Tell(replyMessage);
                }
            }
        }

        public static void Install(ActorSystem system)
        {
            system.ActorOf<DeadRequestProcessingActor>("DeadRequestProcessingActor");
        }
    }
}
