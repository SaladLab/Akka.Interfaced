using Akka.Interfaced;

namespace SlimHttp.Program.Client
{
    internal class SlimActorTarget : IRequestTarget
    {
        public string Id { get; set; }

        public SlimActorTarget()
        {
        }

        public SlimActorTarget(string id)
        {
            Id = id;
        }

        public IRequestWaiter DefaultRequestWaiter => null;
    }
}
