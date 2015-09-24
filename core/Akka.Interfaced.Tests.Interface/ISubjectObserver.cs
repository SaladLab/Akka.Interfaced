using System.Threading.Tasks;

namespace Akka.Interfaced.Tests
{
    public interface ISubjectObserver : IInterfacedObserver
    {
        void Event(string eventName);
    }
}
