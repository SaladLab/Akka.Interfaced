namespace Akka.Interfaced.SlimClient.Tests
{
    public interface ISubjectObserver : IInterfacedObserver
    {
        void Event(string eventName);
    }
}
