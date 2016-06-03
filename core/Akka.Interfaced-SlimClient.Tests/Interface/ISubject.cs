using System;
using System.Threading.Tasks;

namespace Akka.Interfaced.SlimClient.Tests
{
    public interface ISubject : IInterfacedActor
    {
        Task MakeEvent(string eventName);
        Task Subscribe(ISubjectObserver observer);
        Task Unsubscribe(ISubjectObserver observer);
    }
}
