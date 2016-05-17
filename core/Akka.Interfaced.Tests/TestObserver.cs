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

    public class TestObserverActor : InterfacedActor<TestObserverActor>, IDummy, ISubjectObserver
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public TestObserverActor(SubjectRef subject, List<string> eventLog)
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

    public class TestObserverExtendedActor : InterfacedActor<TestObserverExtendedActor>, IDummy, IExtendedInterface<ISubjectObserver>
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public TestObserverExtendedActor(SubjectRef subject, List<string> eventLog)
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

    public class TestObserverExtendedAsyncActor : InterfacedActor<TestObserverExtendedAsyncActor>, IDummy, IExtendedInterface<ISubjectObserver>
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public TestObserverExtendedAsyncActor(SubjectRef subject, List<string> eventLog)
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

    public class TestObserverExtendedAsyncReentrantActor : InterfacedActor<TestObserverExtendedAsyncReentrantActor>, IDummy, IExtendedInterface<ISubjectObserver>
    {
        private SubjectRef _subject;
        private List<string> _eventLog;

        public TestObserverExtendedAsyncReentrantActor(SubjectRef subject, List<string> eventLog)
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

    public class TestObserver : Akka.TestKit.Xunit2.TestKit
    {
        public TestObserver(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Fact]
        public async Task Test_BasicActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<TestObserverActor>(
                Props.Create<TestObserverActor>(subject, eventLog), "TestObserverActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new[] { "A" }, eventLog);
        }

        [Fact]
        public async Task Test_ExtendedActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<TestObserverExtendedActor>(
                Props.Create<TestObserverExtendedActor>(subject, eventLog), "TestObserverExtendedActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new[] { "A" }, eventLog);
        }

        [Fact]
        public async Task Test_ExtendedAsyncActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<TestObserverExtendedAsyncActor>(
                Props.Create<TestObserverExtendedAsyncActor>(subject, eventLog), "TestObserverExtendedAsyncActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(null);

            // Assert
            Assert.Equal(new[] { "A:1", "A:2", "B:1", "B:2" }, eventLog);
        }

        [Fact]
        public async Task Test_ExtendedAsyncReentrantActor_ObserveSubject()
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<TestObserverExtendedAsyncReentrantActor>(
                Props.Create<TestObserverExtendedAsyncReentrantActor>(subject, eventLog), "TestObserverExtendedAsyncReentrantActor");
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
