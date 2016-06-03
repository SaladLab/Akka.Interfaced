using Akka.Interfaced;

namespace Manual
{
    public interface IGreetObserver : IInterfacedObserver
    {
        void Event(string message);
    }
}
