using System;

namespace Akka.Interfaced
{
    public interface IPayloadActorRefUpdatable
    {
        void Update(Action<object> updater);
    }
}
