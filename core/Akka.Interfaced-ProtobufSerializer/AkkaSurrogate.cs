using System;
using Akka.Actor;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Akka.Interfaced.ProtobufSerializer
{
    internal static class AkkaSurrogate
    {
        public static ActorSystem CurrentSystem { get; internal set; }

        [ProtoContract]
        public class ActorPath
        {
            [ProtoMember(1)] public string Path;

            public static readonly Type Target = typeof(Akka.Actor.ActorPath);

            public static implicit operator ActorPath(Akka.Actor.ActorPath value)
            {
                if (value == null)
                    return null;
                var path = ((Akka.Actor.ActorPath.Surrogate)value.ToSurrogate(CurrentSystem)).Path;
                return new ActorPath { Path = path };
            }

            public static implicit operator Akka.Actor.ActorPath(ActorPath surrogate)
            {
                if (surrogate == null)
                    return null;
                var obj = ((new Akka.Actor.ActorPath.Surrogate(surrogate.Path)).FromSurrogate(CurrentSystem));
                return (Akka.Actor.ActorPath)obj;
            }
        }

        [ProtoContract]
        public class Address
        {
            [ProtoMember(1)] public string Protocol;
            [ProtoMember(2)] public string System;
            [ProtoMember(3)] public string Host;
            [ProtoMember(4)] public int? Port;

            public static readonly Type Target = typeof(Akka.Actor.Address);

            public static implicit operator Address(Akka.Actor.Address value)
            {
                if (value == null)
                    return null;
                var obj = new Address
                {
                    Protocol = value.Protocol,
                    System = value.System,
                    Host = value.Host,
                    Port = value.Port,
                };
                return obj;
            }

            public static implicit operator Akka.Actor.Address(Address surrogate)
            {
                if (surrogate == null)
                    return null;
                var obj = new Akka.Actor.Address(surrogate.Protocol, surrogate.System, surrogate.Host, surrogate.Port);
                return obj;
            }
        }

        // At first I want to create a type for surrogating IActorRef.
        // But unfortunately implicit operator conversion doesn't support object to interface conversion.
        // Therefore ActorRefBase was chosen to workaround this limit.

        [ProtoContract]
        public class ActorRefBase
        {
            [ProtoMember(1)] public string Path;

            public static readonly Type Target = typeof(Akka.Actor.ActorRefBase);

            public static implicit operator ActorRefBase(Akka.Actor.ActorRefBase value)
            {
                if (value == null)
                    return null;
                var path = ((Akka.Actor.ActorRefBase.Surrogate)value.ToSurrogate(CurrentSystem)).Path;
                return new ActorRefBase { Path = path };
            }

            public static implicit operator Akka.Actor.ActorRefBase(ActorRefBase surrogate)
            {
                if (surrogate == null)
                    return null;
                var obj = ((new Akka.Actor.ActorRefBase.Surrogate(surrogate.Path)).FromSurrogate(CurrentSystem));
                return (Akka.Actor.ActorRefBase)obj;
            }
        }

        public static void Register(RuntimeTypeModel typeModel)
        {
            typeModel.Add(ActorPath.Target, false).SetSurrogate(typeof(ActorPath));
            typeModel.Add(Address.Target, false).SetSurrogate(typeof(Address));
            typeModel.Add(ActorRefBase.Target, false).SetSurrogate(typeof(ActorRefBase));
        }
    }
}
