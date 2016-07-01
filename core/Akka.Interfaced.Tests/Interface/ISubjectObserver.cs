using System;

namespace Akka.Interfaced
{
    public interface ISubjectObserver : IInterfacedObserver
    {
        void Event(string eventName);
    }

    public interface ISubject2Observer : IInterfacedObserver
    {
        void Event(string eventName);
        void Event2(string eventName);
    }

    public interface ISubjectExObserver : ISubjectObserver
    {
        void EventEx(string eventName);
    }

    public interface ISubjectObserver<T> : IInterfacedObserver
        where T : ICloneable
    {
        void Event(T eventName);
        void Event<U>(T eventName, U eventParam)
            where U : IComparable<U>;
    }
}
