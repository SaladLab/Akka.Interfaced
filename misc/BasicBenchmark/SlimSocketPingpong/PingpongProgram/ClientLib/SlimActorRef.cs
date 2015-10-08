using System;
using Akka.Actor;
using Akka.Util;

namespace PingpongProgram.ClientLib
{
    class SlimActorRef : IActorRef
    {
        public int Id { get; set; }

        #region Dummy method for pretending to be Akka IActorRef

        void ICanTell.Tell(object message, IActorRef sender)
        {
            throw new NotImplementedException();
        }

        bool IEquatable<IActorRef>.Equals(IActorRef other)
        {
            throw new NotImplementedException();
        }

        int IComparable<IActorRef>.CompareTo(IActorRef other)
        {
            throw new NotImplementedException();
        }

        ISurrogate ISurrogated.ToSurrogate(ActorSystem system)
        {
            throw new NotImplementedException();
        }

        ActorPath IActorRef.Path
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
