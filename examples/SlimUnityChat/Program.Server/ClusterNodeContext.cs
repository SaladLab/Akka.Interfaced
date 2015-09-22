using Akka;
using Akka.Actor;
using SlimUnityChat.Interface;

namespace SlimUnityChat.Program.Server
{
    public class ClusterNodeContext
    {
        public ActorSystem System;
        public IActorRef ClusterNodeActor;

        // quick access point for actors. but these are shared variables.
        // if there is a neat way to avoid this dirty hack, please improve it.
        public UserDirectoryRef UserDirectory;
        public RoomDirectoryRef RoomDirectory;
    }
}
