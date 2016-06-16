using System;
using Akka.Actor;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Akka.Interfaced.ProtobufSerializer
{
    public static class AkkaSurrogate
    {
        [ThreadStatic]
        internal static ActorSystem CurrentSystem;

        [ProtoContract]
        public class SurrogateForActorPath
        {
            [ProtoMember(1)] public string Path;

            [ProtoConverter]
            public static SurrogateForActorPath Convert(ActorPath value)
            {
                if (value == null)
                    return null;

                var path = ((ActorPath.Surrogate)value.ToSurrogate(CurrentSystem)).Path;
                return new SurrogateForActorPath { Path = path };
            }

            [ProtoConverter]
            public static ActorPath Convert(SurrogateForActorPath value)
            {
                if (value == null)
                    return null;

                var obj = ((new ActorPath.Surrogate(value.Path)).FromSurrogate(CurrentSystem));
                return (ActorPath)obj;
            }
        }

        [ProtoContract]
        public class SurrogateForAddress
        {
            [ProtoMember(1)] public string Protocol;
            [ProtoMember(2)] public string System;
            [ProtoMember(3)] public string Host;
            [ProtoMember(4)] public int? Port;

            [ProtoConverter]
            public static SurrogateForAddress Convert(Address value)
            {
                if (value == null)
                    return null;

                return new SurrogateForAddress
                {
                    Protocol = value.Protocol,
                    System = value.System,
                    Host = value.Host,
                    Port = value.Port,
                };
            }

            [ProtoConverter]
            public static Address Convert(SurrogateForAddress value)
            {
                if (value == null)
                    return null;

                return new Address(value.Protocol, value.System, value.Host, value.Port);
            }
        }

        [ProtoContract]
        public class SurrogateForIActorRef
        {
            [ProtoMember(1)] public string Path;

            [ProtoConverter]
            public static SurrogateForIActorRef Convert(IActorRef value)
            {
                if (value == null)
                    return null;

                var path = ((ActorRefBase.Surrogate)value.ToSurrogate(CurrentSystem)).Path;
                return new SurrogateForIActorRef { Path = path };
            }

            [ProtoConverter]
            public static IActorRef Convert(SurrogateForIActorRef value)
            {
                if (value == null)
                    return null;

                var obj = ((new ActorRefBase.Surrogate(value.Path)).FromSurrogate(CurrentSystem));
                return (IActorRef)obj;
            }
        }

        [ProtoContract]
        public class SurrogateForIRequestTarget
        {
            // Alwasy assumes that IRequestTarget is AkkaActorTarget
            // because this serializer cannot be used under SlimClient.

            [ProtoMember(1)]
            public string Path;

            [ProtoConverter]
            public static SurrogateForIRequestTarget Convert(IRequestTarget value)
            {
                if (value == null)
                    return null;

                var actor = ((AkkaActorTarget)value).Actor;
                var path = ((ActorRefBase.Surrogate)actor.ToSurrogate(CurrentSystem)).Path;
                return new SurrogateForIRequestTarget { Path = path };
            }

            [ProtoConverter]
            public static IRequestTarget Convert(SurrogateForIRequestTarget value)
            {
                if (value == null)
                    return null;

                var actor = (IActorRef)((new ActorRefBase.Surrogate(value.Path)).FromSurrogate(CurrentSystem));
                return new AkkaActorTarget(actor);
            }
        }

        [ProtoContract]
        public class SurrogateForINotificationChannel
        {
            [ProtoMember(1)] public IActorRef Actor;

            [ProtoConverter]
            public static SurrogateForINotificationChannel Convert(INotificationChannel value)
            {
                if (value == null)
                    return null;

                var actor = ((ActorNotificationChannel)value).Actor;
                return new SurrogateForINotificationChannel { Actor = actor };
            }

            [ProtoConverter]
            public static INotificationChannel Convert(SurrogateForINotificationChannel value)
            {
                if (value == null)
                    return null;

                return new ActorNotificationChannel(value.Actor);
            }
        }

        public static void Register(RuntimeTypeModel typeModel)
        {
            typeModel.Add(typeof(ActorPath), false).SetSurrogate(typeof(SurrogateForActorPath));
            typeModel.Add(typeof(Address), false).SetSurrogate(typeof(SurrogateForAddress));
            typeModel.Add(typeof(IActorRef), false).SetSurrogate(typeof(SurrogateForIActorRef));
            typeModel.Add(typeof(INotificationChannel), false).SetSurrogate(typeof(SurrogateForINotificationChannel));
        }
    }
}
