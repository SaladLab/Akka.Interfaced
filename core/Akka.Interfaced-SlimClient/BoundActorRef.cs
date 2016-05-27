namespace Akka.Interfaced
{
    public class BoundActorRef : IActorRef
    {
        public int Id { get; set; }

        public BoundActorRef()
        {
        }

        public BoundActorRef(int id)
        {
            Id = id;
        }
    }
}
