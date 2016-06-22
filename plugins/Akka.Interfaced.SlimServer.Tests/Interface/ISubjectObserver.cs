using System.Threading.Tasks;

namespace Akka.Interfaced.SlimServer
{
    public interface ISubjectObserver : IInterfacedObserver
    {
        void Event(string eventName);
    }
}
