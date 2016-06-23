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

            var target = new AkkaActorTarget(actorRef.Actor);
            var newActorRef = new TRef { Target = target, RequestWaiter = target.DefaultRequestWaiter };
            CheckIfActorImplementsOrThrow(actorRef.Type, newActorRef.InterfaceType);

            return newActorRef;
        }

        // Wrap actor-ref into TRef (not type-safe cast)

        public static TRef Cast<TRef>(this IActorRef actorRef)
            where TRef : InterfacedActorRef, new()
        {
            if (actorRef == null)
                return null;

            var target = new AkkaActorTarget(actorRef);
            return new TRef { Target = target, RequestWaiter = target.DefaultRequestWaiter };
        }

        // Unwrap actor-ref from TRef

        public static IActorRef CastToIActorRef(this InterfacedActorRef actor)
        {
            if (actor == null || actor.Target == null)
                return null;

            return ((AkkaActorTarget)actor.Target).Actor;
        }
    }
}
