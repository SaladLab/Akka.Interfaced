using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using System;

namespace Akka.Interfaced.Tests
{
    public class SubjectActor : InterfacedActor<SubjectActor>, ISubject
    {
        private List<ISubjectObserver> _observers = new List<ISubjectObserver>();
            
        async Task ISubject.MakeEvent(string eventName)
        {
            await Task.Delay(10);

            foreach (var observer in _observers)
                observer.Event(eventName);
        }

        Task ISubject.Subscribe(ISubjectObserver observer)
        {
            _observers.Add(observer);
            return Task.FromResult(0);
        }

        Task ISubject.Unsubscribe(ISubjectObserver observer)
        {
            _observers.Remove(observer);
            return Task.FromResult(0);
        }
    }

    public class TestObserverActor : InterfacedActor<TestObserverActor>, IDummy, ISubjectObserver
    {
        private SubjectRef _subject;
        private List<object> _eventLog;

        public TestObserverActor(SubjectRef subject, List<object> eventLog)
        {
            _subject = subject.WithRequestWaiter(this);
            _eventLog = eventLog;
        }

        [Reentrant]
        async Task<object> IDummy.Call(object param)
        {
            var id = IssueObserverId();
            AddObserver(id, this);
            await _subject.Subscribe(new SubjectObserver(Self, id));
            await _subject.MakeEvent("A");
            await _subject.Unsubscribe(new SubjectObserver(Self, id));
            await _subject.MakeEvent("B");
            RemoveObserver(1);
            return null;
        }

        void ISubjectObserver.Event(string eventName)
        {
            _eventLog.Add(eventName);
        }
    }

    public class TestObserver : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public async Task Test_BasicActor_ThrowException_NoThrow()
        {
            // Arrange
            var eventLog = new List<object>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>();
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<TestObserverActor>(Props.Create<TestObserverActor>(subject, eventLog));
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new List<object> { "A" }, eventLog);
        }
    }
}
