namespace Akka.Interfaced
{
    public interface IRequestTarget
    {
        IRequestWaiter DefaultRequestWaiter { get; }
    }
}
