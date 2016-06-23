using System;
using System.Linq;
using System.Linq.Expressions;
using Akka.Actor;

namespace Akka.Interfaced
{
    public struct TypedActorRef
    {
        public IActorRef Actor;
        public Type Type;
    }

    public static class InterfacedActorOfExtensions
    {
        public static TypedActorRef InterfacedActorOf(this IActorRefFactory factory, Props props, string name = null)
        {
            return new TypedActorRef { Actor = factory.ActorOf(props, name), Type = props.Type };
        }

        public static TypedActorRef InterfacedActorOf<TActor>(this IActorRefFactory factory, string name = null)
            where TActor : InterfacedActor, new()
        {
            return new TypedActorRef { Actor = factory.ActorOf<TActor>(name), Type = typeof(TActor) };
        }

        public static TypedActorRef InterfacedActorOf<TActor>(this IActorRefFactory factory, Expression<Func<TActor>> propFactory, SupervisorStrategy supervisorStrategy = null, string name = null)
            where TActor : InterfacedActor
        {
            return new TypedActorRef { Actor = factory.ActorOf(Props.Create(propFactory, supervisorStrategy), name), Type = typeof(TActor) };
        }

        public static bool CheckIfActorImplements(Type actorType, Type interfaceType)
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

        public static void CheckIfActorImplementsOrThrow(Type actorType, Type interfaceType)
        {
            if (CheckIfActorImplements(actorType, interfaceType) == false)
                throw new InvalidCastException();
        }
    }
}
