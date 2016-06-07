using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    public class SubjectActor : InterfacedActor, ISubject
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

    public class Subject2Actor : InterfacedActor, ISubject2
    {
        private List<ISubject2Observer> _observers = new List<ISubject2Observer>();

        async Task ISubject2.MakeEvent(string eventName)
        {
            await Task.Delay(10);

            foreach (var observer in _observers)
                observer.Event(eventName);
        }

        async Task ISubject2.MakeEvent2(string eventName)
        {
            await Task.Delay(10);

            foreach (var observer in _observers)
                observer.Event2(eventName);
        }

        Task ISubject2.Subscribe(ISubject2Observer observer)
        {
            _observers.Add(observer);
            return Task.FromResult(0);
        }

        Task ISubject2.Unsubscribe(ISubject2Observer observer)
        {
            _observers.Remove(observer);
            return Task.FromResult(0);
        }
    }

    public class ObserverActor : InterfacedActor, IDummy, ISubjectObserver
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
            var observer = CreateObserver<ISubjectObserver>(param);
            await _subject.Subscribe(observer);
            await _subject.MakeEvent("A");
            await _subject.Unsubscribe(observer);
            await _subject.MakeEvent("B");
            RemoveObserver(observer);
            return null;
        }

        void ISubjectObserver.Event(string eventName)
        {
            var c = ObserverContext != null ? ObserverContext + ":" : "";
            _eventLog.Add(c + eventName);
        }
    }

    public class ObserverExtendedActor : InterfacedActor, IDummy, IExtendedInterface<ISubjectObserver>
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
            var observer = CreateObserver<ISubjectObserver>(param);
            await _subject.Subscribe(observer);
            await _subject.MakeEvent("A");
            await _subject.Unsubscribe(observer);
            await _subject.MakeEvent("B");
            RemoveObserver(observer);
            return null;
        }

        [ExtendedHandler]
        private void Event(string eventName)
        {
            var c = ObserverContext != null ? ObserverContext + ":" : "";
            _eventLog.Add(c + eventName);
        }
    }

    public class ObserverExtendedAsyncActor : InterfacedActor, IDummy, IExtendedInterface<ISubjectObserver>
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
            var observer = CreateObserver<ISubjectObserver>(param);
            await _subject.Subscribe(observer);
            await _subject.MakeEvent("A");
            await _subject.MakeEvent("B");
            await _subject.Unsubscribe(observer);
            RemoveObserver(observer);
            return null;
        }

        [ExtendedHandler]
        private async Task Event(string eventName)
        {
            var c = ObserverContext != null ? ObserverContext + ":" : "";
            _eventLog.Add(c + eventName + ":1");

            await Task.Delay(10);

            var contextMessage2 = ObserverContext != null ? ObserverContext + ":" : "";
            _eventLog.Add(contextMessage2 + eventName + ":2");
        }
    }

    public class ObserverExtendedAsyncReentrantActor : InterfacedActor, IDummy, IExtendedInterface<ISubjectObserver>
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
            var observer = CreateObserver<ISubjectObserver>(param);
            await _subject.Subscribe(observer);
            await _subject.MakeEvent("A");
            await _subject.MakeEvent("B");
            await _subject.Unsubscribe(observer);
            RemoveObserver(observer);
            return null;
        }

        [ExtendedHandler, Reentrant]
        private async Task Event(string eventName)
        {
            var c = ObserverContext != null ? ObserverContext + ":" : "";
            _eventLog.Add(c + eventName + ":1");

            await Task.Delay(100);

            var contextMessage2 = ObserverContext != null ? ObserverContext + ":" : "";
            _eventLog.Add(contextMessage2 + eventName + ":2");
        }
    }

    public class Observer : Akka.TestKit.Xunit2.TestKit
    {
        public Observer(ITestOutputHelper output)
            : base(output: output)
        {
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task BasicActor_ObserveSubject(object context)
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverActor>(
                Props.Create<ObserverActor>(subject, eventLog), "TestObserverActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(context);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A" }, eventLog);
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task ExtendedActor_ObserveSubject(object context)
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverExtendedActor>(
                Props.Create<ObserverExtendedActor>(subject, eventLog), "TestObserverExtendedActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(context);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A" }, eventLog);
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task ExtendedAsyncActor_ObserveSubject(object context)
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverExtendedAsyncActor>(
                Props.Create<ObserverExtendedAsyncActor>(subject, eventLog), "TestObserverExtendedAsyncActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(context);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A:1", $"{c}A:2", $"{c}B:1", $"{c}B:2" }, eventLog);
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task ExtendedAsyncReentrantActor_ObserveSubject(object context)
        {
            // Arrange
            var eventLog = new List<string>();
            var subjectActor = ActorOfAsTestActorRef<SubjectActor>("SubjectActor");
            var subject = new SubjectRef(subjectActor);
            var observerActor = ActorOfAsTestActorRef<ObserverExtendedAsyncReentrantActor>(
                Props.Create<ObserverExtendedAsyncReentrantActor>(subject, eventLog), "TestObserverExtendedAsyncReentrantActor");
            var observer = new DummyRef(observerActor);

            // Act
            await observer.Call(context);
            await Task.Delay(200);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A:1", $"{c}A:2" }, eventLog.Where(x => x.StartsWith($"{c}A")));
            Assert.Equal(new[] { $"{c}B:1", $"{c}B:2" }, eventLog.Where(x => x.StartsWith($"{c}B")));
        }
    }
}
