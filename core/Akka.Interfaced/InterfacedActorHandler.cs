using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Akka.Interfaced
{
    public class InterfacedActorHandler
    {
        public readonly RequestDispatcher RequestDispatcher;
        public readonly NotificationDispatcher NotificationDispatcher;
        public readonly MessageDispatcher MessageDispatcher;
        public readonly List<Func<object, IFilter>> PerInstanceFilterCreators;

        public InterfacedActorHandler(Type type)
        {
            var filterHandlerBuilder = new FilterHandlerBuilder(type);

            var requestHandlerBuilder = new RequestHandlerBuilder();
            RequestDispatcher = new RequestDispatcher(
                requestHandlerBuilder.Build(type, filterHandlerBuilder));

            var notificationHandlerBuilder = new NotificationHandlerBuilder();
            NotificationDispatcher = new NotificationDispatcher(
                notificationHandlerBuilder.Build(type, filterHandlerBuilder));

            var messageHandlerBuilder = new MessageHandlerBuilder();
            MessageDispatcher = new MessageDispatcher(
                messageHandlerBuilder.Build(type, filterHandlerBuilder));

            PerInstanceFilterCreators = filterHandlerBuilder.PerInstanceFilterCreators;
        }
    }

    public static class InterfacedActorHandlerTable
    {
        private static ConcurrentDictionary<Type, InterfacedActorHandler> s_table =
            new ConcurrentDictionary<Type, InterfacedActorHandler>();

        public static InterfacedActorHandler Get(Type type)
        {
            return s_table.GetOrAdd(type, t => new InterfacedActorHandler(t));
        }
    }
}
