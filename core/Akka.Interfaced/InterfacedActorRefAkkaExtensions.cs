using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Interfaced
{
    public static class InterfacedActorRefAkkaExtensions
    {
        internal static bool CheckIfActorImplements(Type actorType, Type interfaceType)
        {
            // implements the canonical interface ?
            if (interfaceType.IsAssignableFrom(actorType))
                return true;

            // implements the synchronous interface ?
            var syncInterfaceType = interfaceType.Assembly.GetType(interfaceType.FullName + "Sync");
            if (syncInterfaceType.IsAssignableFrom(actorType))
                return true;

            // implements the extended interface ?
            var extendedTypes = actorType.GetInterfaces()
                .Where(t => t.FullName.StartsWith("Akka.Interfaced.IExtendedInterface"))
                .SelectMany(t => t.GenericTypeArguments);
            foreach (var extendedType in extendedTypes)
            {
                if (interfaceType.IsAssignableFrom(extendedType))
                    return true;
            }

            return false;
        }

        internal static void CheckIfActorImplementsOrThrow(Type actorType, Type interfaceType)
        {
            if (CheckIfActorImplements(actorType, interfaceType) == false)
                throw new InvalidCastException();
        }

        // Wrap typed actor-ref into TRef (type-safe)

        public static TRef Cast<TRef>(this TypedActorRef actorRef)
            where TRef : InterfacedActorRef, new()
        {
            if (actorRef.Actor == null)
                return null;

            var target = new AkkaReceiverTarget(actorRef.Actor);
            var newActorRef = new TRef { Target = target, RequestWaiter = target.DefaultRequestWaiter };
            CheckIfActorImplementsOrThrow(actorRef.Type, newActorRef.InterfaceType);

            return newActorRef;
        }

        // Wrap ICanTell into TRef (not type-safe cast)

        public static TRef Cast<TRef>(this ICanTell receiver)
            where TRef : InterfacedActorRef, new()
        {
            if (receiver == null)
                return null;

            var target = new AkkaReceiverTarget(receiver);
            return new TRef { Target = target, RequestWaiter = target.DefaultRequestWaiter };
        }

        // Unwrap ICanTell from TRef

        public static ICanTell CastToICanTell(this InterfacedActorRef actor)
        {
            if (actor == null || actor.Target == null)
                return null;

            return ((AkkaReceiverTarget)actor.Target).Receiver;
        }

        // Unwrap IActorRef from TRef

        public static IActorRef CastToIActorRef(this InterfacedActorRef actor)
        {
            return (IActorRef)(actor.CastToICanTell());
        }
    }
}
