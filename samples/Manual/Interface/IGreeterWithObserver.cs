using System.Threading.Tasks;
using Akka.Interfaced;

namespace Manual
{
    public interface IGreeterWithObserver : IInterfacedActor
    {
        Task<string> Greet(string name);
        Task<int> GetCount();

        // add an observer which receives a notification message whenever Greet request comes in
        Task Subscribe(IGreetObserver observer);

        // remove an observer
        Task Unsubscribe(IGreetObserver observer);
    }
}
