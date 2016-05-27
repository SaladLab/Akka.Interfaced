using System;

namespace Akka.Interfaced
{
    public interface IPayloadObserverUpdatable
    {
        void Update(Action<IInterfacedObserver> updater);
    }
}
