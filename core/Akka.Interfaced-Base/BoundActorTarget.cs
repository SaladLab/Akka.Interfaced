namespace Akka.Interfaced
{
    public class BoundActorTarget : IRequestTarget
    {
        public int Id { get; set; }

        public BoundActorTarget()
        {
        }

        public BoundActorTarget(int id)
        {
            Id = id;
        }

        public IRequestWaiter DefaultRequestWaiter => null;
    }
}
