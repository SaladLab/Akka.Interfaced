using System;
using System.Threading.Tasks;

namespace Akka.Interfaced
{
    public abstract class InterfacedActorRef
    {
        public IRequestTarget Target { get; protected internal set; }
        public IRequestWaiter RequestWaiter { get; protected internal set; }
        public TimeSpan? Timeout { get; protected internal set; }

        abstract public Type InterfaceType { get; }

        protected InterfacedActorRef(IRequestTarget target)
        {
            Target = target;
            RequestWaiter = target?.DefaultRequestWaiter;
        }

        protected InterfacedActorRef(IRequestTarget target, IRequestWaiter requestWaiter, TimeSpan? timeout = null)
        {
            Target = target;
            RequestWaiter = requestWaiter;
            Timeout = timeout;
        }

        // Request & Response

        protected void SendRequest(RequestMessage requestMessage)
        {
            RequestWaiter.SendRequest(Target, requestMessage);
        }

        protected Task SendRequestAndWait(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndWait(Target, requestMessage, Timeout);
        }

        protected Task<TReturn> SendRequestAndReceive<TReturn>(RequestMessage requestMessage)
        {
            return RequestWaiter.SendRequestAndReceive<TReturn>(Target, requestMessage, Timeout);
        }

        public static InterfacedActorRef Create(Type type)
        {
            if (type.IsInterface)
            {
                // Namespace.IExampleActor -> Namespace.ExampleActorRef
                var proxyTypeName = (type.Namespace.Length > 0 ? type.Namespace + "." : "") + type.Name.Substring(1) + "Ref";
                var proxyType = type.Assembly.GetType(proxyTypeName);
                if (proxyType != null && proxyType.IsGenericType)
                    proxyType = proxyType.MakeGenericType(type.GetGenericArguments());
                if (proxyType == null || proxyType.BaseType != typeof(InterfacedActorRef))
                    throw new ArgumentException("Cannot resolve the InterfacedActorRef type from " + type.FullName);

                var proxy = Activator.CreateInstance(proxyType);
                return (InterfacedActorRef)proxy;
            }
            else if (type.IsClass)
            {
                // Namespace.ExampleObserver
                if (type.BaseType != typeof(InterfacedActorRef))
                    throw new ArgumentException("Cannot create InterfacedActorRef with " + type.FullName);

                var proxy = Activator.CreateInstance(type);
                return (InterfacedActorRef)proxy;
            }
            else
            {
                throw new ArgumentException("Cannot create InterfacedActorRef from " + type.FullName);
            }
        }
    }

    // Internal use only
    public static class InterfacedActorRefModifier
    {
        public static void SetTarget(InterfacedActorRef actorRef, IRequestTarget target)
        {
            actorRef.Target = target;
        }

        public static void SetRequestWaiter(InterfacedActorRef actorRef, IRequestWaiter requestWaiter)
        {
            actorRef.RequestWaiter = requestWaiter;
        }

        public static void SetTimeout(InterfacedActorRef actorRef, TimeSpan? timeout)
        {
            actorRef.Timeout = timeout;
        }
    }
}
