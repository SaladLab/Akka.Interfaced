using System;
using System.Collections.Generic;

namespace Akka.Interfaced
{
    public class SubjectActor : InterfacedActor, ISubjectSync
    {
        private List<ISubjectObserver> _observers = new List<ISubjectObserver>();

        void ISubjectSync.MakeEvent(string eventName)
        {
            foreach (var observer in _observers)
                observer.Event(eventName);
        }

        void ISubjectSync.Subscribe(ISubjectObserver observer)
        {
            _observers.Add(observer);
        }

        void ISubjectSync.Unsubscribe(ISubjectObserver observer)
        {
            _observers.Remove(observer);
        }
    }

    public class Subject2Actor : InterfacedActor, ISubject2Sync
    {
        private List<ISubject2Observer> _observers = new List<ISubject2Observer>();

        void ISubject2Sync.MakeEvent(string eventName)
        {
            foreach (var observer in _observers)
                observer.Event(eventName);
        }

        void ISubject2Sync.MakeEvent2(string eventName)
        {
            foreach (var observer in _observers)
                observer.Event2(eventName);
        }

        void ISubject2Sync.Subscribe(ISubject2Observer observer)
        {
            _observers.Add(observer);
        }

        void ISubject2Sync.Unsubscribe(ISubject2Observer observer)
        {
            _observers.Remove(observer);
        }
    }

    public class SubjectExActor : InterfacedActor, ISubjectExSync
    {
        private List<ISubjectExObserver> _observers = new List<ISubjectExObserver>();

        void ISubjectExSync.MakeEvent(string eventName)
        {
            foreach (var observer in _observers)
                observer.Event(eventName);
        }

        void ISubjectExSync.MakeEventEx(string eventName)
        {
            foreach (var observer in _observers)
                observer.Event(eventName);
        }

        void ISubjectExSync.Subscribe(ISubjectExObserver observer)
        {
            _observers.Add(observer);
        }

        void ISubjectExSync.Unsubscribe(ISubjectExObserver observer)
        {
            _observers.Remove(observer);
        }
    }
}
