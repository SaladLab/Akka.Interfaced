namespace Akka.Interfaced
{
    public class BoundActorTarget : IRequestTarget
    {
        public int Id { get; set; }
        public string Address { get; set; }

        public BoundActorTarget()
        {
        }

        public BoundActorTarget(int id, string address = null)
        {
            Id = id;
            Address = address;
        }

        public IRequestWaiter DefaultRequestWaiter => null;
    }
}
