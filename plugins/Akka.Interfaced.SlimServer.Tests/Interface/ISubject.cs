using System.Threading.Tasks;

namespace Akka.Interfaced.SlimServer
{
    public interface ISubject : IInterfacedActor
    {
        Task MakeEvent(string eventName);
        Task Subscribe(ISubjectObserver observer);
        Task Unsubscribe(ISubjectObserver observer);
    }
}
