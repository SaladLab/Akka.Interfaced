using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface ISubjectObserver : IInterfacedObserver
    {
        void Event(string eventName);
    }

    public interface ISubject2Observer : IInterfacedObserver
    {
        void Event(string eventName);
        void Event2(string eventName);
    }

    public interface ISubjectExObserver : ISubjectObserver
    {
        void EventEx(string eventName);
    }
}
