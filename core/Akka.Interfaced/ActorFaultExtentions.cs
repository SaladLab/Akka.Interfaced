using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;

namespace Akka.Interfaced
{
    internal static class ActorFaultExtentions
    {
        private static MethodInfo s_handleInvokeFailure =
            typeof(ActorCell).GetMethod("HandleInvokeFailure", BindingFlags.NonPublic | BindingFlags.Instance);

        // Hack for calling ActorCell.HandleInvokeFailure for notifying exception thrown manually.
        public static void InvokeFailure(this ActorCell actorCell, Exception cause)
        {
            s_handleInvokeFailure.Invoke(actorCell, new object[] { cause, (IEnumerable<IActorRef>)null });
        }
    }
}
