using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Basic.Interface;

namespace Basic.Program
{
    public class TestEventGenerator : InterfacedActor, IEventGenerator
    {
        private HashSet<IEventObserver> _observers = new HashSet<IEventObserver>();

        Task IEventGenerator.Subscribe(IEventObserver observer)
        {
            _observers.Add(observer);
            return Task.FromResult(true);
        }

        Task IEventGenerator.Unsubscribe(IEventObserver observer)
        {
            _observers.Remove(observer);
            return Task.FromResult(true);
        }

        Task IEventGenerator.Buy(string name, int price)
        {
            Console.WriteLine("IEventGenerator.Buy({0}, {1})", name, price);
            foreach (var observer in _observers)
                observer.OnBuy(name, price);
            return Task.FromResult(true);
        }

        Task IEventGenerator.Sell(string name, int price)
        {
            Console.WriteLine("IEventGenerator.Sell({0}, {1})", name, price);
            foreach (var observer in _observers)
                observer.OnSell(name, price);
            return Task.FromResult(true);
        }
    }
}
