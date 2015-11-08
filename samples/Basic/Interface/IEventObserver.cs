using System.Threading.Tasks;
using Akka.Interfaced;

namespace Basic.Interface
{
    public interface IEventObserver : IInterfacedObserver
    {
        void OnBuy(string name, int price);
        void OnSell(string name, int price);
    }
}
