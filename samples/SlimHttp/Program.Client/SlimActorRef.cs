using Akka.Interfaced;

namespace SlimHttp.Program.Client
{
    internal class SlimActorRef : IActorRef
    {
        public string Id { get; set; }

        public SlimActorRef()
        {
        }

        public SlimActorRef(string id)
        {
            Id = id;
        }
    }
}
