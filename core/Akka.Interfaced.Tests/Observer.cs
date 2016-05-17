using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

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

    public class ObserverActor : InterfacedActor<ObserverActor>, IDummy, ISubjectObserver
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public ObserverActor(SubjectRef subject, List<string> eventLog)
        {
            _subject = subject.WithRequestWaiter(this);
            _eventLog = eventLog;
        }

        [Reentrant]
        async Task<object> IDummy.Call(object param)
        {
            var self = Self; // keep Self safely
            await _subject.Subscribe(new SubjectObserver(self));
            await _subject.MakeEvent("A");
            await _subject.Unsubscribe(new SubjectObserver(self));
            await _subject.MakeEvent("B");
            return null;
        }

        void ISubjectObserver.Event(string eventName)
        {
            _eventLog.Add(eventName);
        }
    }

    public class ObserverExtendedActor : InterfacedActor<ObserverExtendedActor>, IDummy, IExtendedInterface<ISubjectObserver>
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public ObserverExtendedActor(SubjectRef subject, List<string> eventLog)
        {
            _subject = subject.WithRequestWaiter(this);
            _eventLog = eventLog;
        }

        [Reentrant]
        async Task<object> IDummy.Call(object param)
        {
            var self = Self; // keep Self safely
            await _subject.Subscribe(new SubjectObserver(self));
            await _subject.MakeEvent("A");
            await _subject.Unsubscribe(new SubjectObserver(self));
            await _subject.MakeEvent("B");
            return null;
        }

        [ExtendedHandler]
        private void Event(string eventName)
        {
            _eventLog.Add(eventName);
        }
    }

    public class ObserverExtendedAsyncActor : InterfacedActor<ObserverExtendedAsyncActor>, IDummy, IExtendedInterface<ISubjectObserver>
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public ObserverExtendedAsyncActor(SubjectRef subject, List<string> eventLog)
        {
            _subject = subject.WithRequestWaiter(this);
            _eventLog = eventLog;
        }

        [Reentrant]
        async Task<object> IDummy.Call(object param)
        {
            var self = Self; // keep Self safely
            await _subject.Subscribe(new SubjectObserver(self));
            await _subject.MakeEvent("A");
            await _subject.MakeEvent("B");
            await _subject.Unsubscribe(new SubjectObserver(self));
            return null;
        }

        [ExtendedHandler]
        private async Task Event(string eventName)
        {
            _eventLog.Add(eventName + ":1");
            await Task.Delay(10);
            _eventLog.Add(eventName + ":2");
        }
    }

    public class ObserverExtendedAsyncReentrantActor : InterfacedActor<ObserverExtendedAsyncReentrantActor>, IDummy, IExtendedInterface<ISubjectObserver>
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public ObserverExtendedAsyncReentrantActor(SubjectRef subject, List<string> eventLog)
        {
            _subject = subject.WithRequestWaiter(this);
            _eventLog = eventLog;
        }

        [Reentrant]
        async Task<object> IDummy.Call(object param)
        {
            var self = Self; // keep Self safely
            await _subject.Subscribe(new SubjectObserver(self));
            await _subject.MakeEvent("A");
            await _subject.MakeEvent("B");
            await _subject.Unsubscribe(new SubjectObserver(self));
            return null;
        }

        [ExtendedHandler, Reentrant]
        private async Task Event(string eventName)
        {
            _eventLog.Add(eventName + ":1");
            await Task.Delay(100);
            _eventLog.Add(eventName + ":2");
        }
    }

    public class Observer : Akka.TestKit.Xunit2.TestKit
    {
        public Observer(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task BasicActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverActor>(
                Props.Create<ObserverActor>(subject, eventLog), "TestObserverActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new[] { "A" }, eventLog);
        }

        [Fact]
        public async Task ExtendedActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverExtendedActor>(
                Props.Create<ObserverExtendedActor>(subject, eventLog), "TestObserverExtendedActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new[] { "A" }, eventLog);
        }

        [Fact]
        public async Task ExtendedAsyncActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverExtendedAsyncActor>(
                Props.Create<ObserverExtendedAsyncActor>(subject, eventLog), "TestObserverExtendedAsyncActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new[] { "A:1", "A:2", "B:1", "B:2" }, eventLog);
        }

        [Fact]
        public async Task ExtendedAsyncReentrantActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverExtendedAsyncReentrantActor>(
                Props.Create<ObserverExtendedAsyncReentrantActor>(subject, eventLog), "TestObserverExtendedAsyncReentrantActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);
            await Task.Delay(200);

            // Assert
            Assert.Equal(new[] { "A:1", "A:2" }, eventLog.Where(x => x.StartsWith("A")));
            Assert.Equal(new[] { "B:1", "B:2" }, eventLog.Where(x => x.StartsWith("B")));
        }
    }
}
