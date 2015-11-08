using System.Threading.Tasks;
using Akka.Interfaced;

namespace Basic.Interface
{
    public interface IEventGenerator : IInterfacedActor
    {
        Task Subscribe(IEventObserver observer);
        Task Unsubscribe(IEventObserver observer);
        Task Buy(string name, int price);
        Task Sell(string name, int price);
    }
}
