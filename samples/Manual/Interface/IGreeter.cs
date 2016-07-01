using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Manual
{
    public interface IGreeter : IInterfacedActor
    {
        Task<string> Greet(string name);
        Task<int> GetCount();
    }

    public interface IGreeter<T> : IInterfacedActor
        where T : ICloneable
    {
        Task<T> Greet(T name);
        Task<T> Greet<U>(U name)
            where U : IComparable<U>;
        Task<int> GetCount();
    }

    public interface IGreeterWithObserver : IGreeter
    {
        // add an observer which receives a notification message whenever Greet request comes in
        Task Subscribe(IGreetObserver observer);

        // remove an observer
        Task Unsubscribe(IGreetObserver observer);
    }
}
