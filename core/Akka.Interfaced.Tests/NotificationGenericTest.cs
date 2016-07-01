using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class NotificationGenericTest : TestKit.Xunit2.TestKit
    {
        public NotificationGenericTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public abstract class TestObserverActorBase : InterfacedActor, IDummy
        {
            private SubjectRef<string> _subject;
            private LogBoard<string> _log;

            public TestObserverActorBase(SubjectRef<string> subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
            }

            [Reentrant]
            async Task<object> IDummy.Call(object param)
            {
                var observer = CreateObserver<ISubjectObserver<string>>(param);
                await _subject.Subscribe(observer);
                await _subject.MakeEvent("A");
                await _subject.MakeEvent("B", 10);
                await _subject.Unsubscribe(observer);
                await _subject.MakeEvent("C");
                RemoveObserver(observer);
                return null;
            }

            protected bool HandleEvent<T>(T eventName)
            {
                var c = ObserverContext != null ? ObserverContext + ":" : "";
                _log.Add(c + eventName);
                return true;
            }

            protected bool HandleEvent<T, U>(T eventName, U eventParam)
            {
                var c = ObserverContext != null ? ObserverContext + ":" : "";
                _log.Add(c + eventName + "," + eventParam);
                return true;
            }
        }

        // Regular Interface & Generic Method

        public class TestObserverActor : TestObserverActorBase, ISubjectObserver<string>
        {
            public TestObserverActor(SubjectRef<string> subject, LogBoard<string> log)
                : base(subject, log) { }

            void ISubjectObserver<string>.Event(string eventName) => HandleEvent(eventName);
            void ISubjectObserver<string>.Event<U>(string eventName, U eventParam) => HandleEvent(eventName, eventParam);
        }

        public class TestObserverAsyncActor : TestObserverActorBase, ISubjectObserverAsync<string>
        {
            public TestObserverAsyncActor(SubjectRef<string> subject, LogBoard<string> log)
                : base(subject, log) { }

            Task ISubjectObserverAsync<string>.Event(string eventName) => Task.FromResult(HandleEvent(eventName));
            Task ISubjectObserverAsync<string>.Event<U>(string eventName, U eventParam) => Task.FromResult(HandleEvent(eventName, eventParam));
        }

        public class TestObserverExtendedActor : TestObserverActorBase, IExtendedInterface<ISubjectObserver<string>>
        {
            public TestObserverExtendedActor(SubjectRef<string> subject, LogBoard<string> log)
                : base(subject, log) { }

            [ExtendedHandler]
            private void Event(string eventName) => HandleEvent(eventName);

            [ExtendedHandler]
            private Task Event<U>(string eventName, U eventParam) => Task.FromResult(HandleEvent(eventName, eventParam));
        }

        // Generic Interface & Generic Method

        public class TestObserverActor<T> : TestObserverActorBase, ISubjectObserver<T>
            where T : ICloneable
        {
            public TestObserverActor(SubjectRef<string> subject, LogBoard<string> log)
                : base(subject, log) { }

            void ISubjectObserver<T>.Event(T eventName) => HandleEvent(eventName);
            void ISubjectObserver<T>.Event<U>(T eventName, U eventParam) => HandleEvent(eventName, eventParam);
        }

        public class TestObserverAsyncActor<T> : TestObserverActorBase, ISubjectObserverAsync<T>
            where T : ICloneable
        {
            public TestObserverAsyncActor(SubjectRef<string> subject, LogBoard<string> log)
                : base(subject, log) { }

            Task ISubjectObserverAsync<T>.Event(T eventName) => Task.FromResult(HandleEvent(eventName));
            Task ISubjectObserverAsync<T>.Event<U>(T eventName, U eventParam) => Task.FromResult(HandleEvent(eventName, eventParam));
        }

        public class TestObserverExtendedActor<T> : TestObserverActorBase, IExtendedInterface<ISubjectObserver<T>>
            where T : ICloneable
        {
            public TestObserverExtendedActor(SubjectRef<string> subject, LogBoard<string> log)
                : base(subject, log) { }

            [ExtendedHandler]
            private void Event(T eventName) => HandleEvent(eventName);

            [ExtendedHandler]
            private Task Event<U>(T eventName, U eventParam) => Task.FromResult(HandleEvent(eventName, eventParam));
        }

        [Theory]
        [InlineData(typeof(TestObserverActor))]
        [InlineData(typeof(TestObserverAsyncActor))]
        [InlineData(typeof(TestObserverExtendedActor))]
        [InlineData(typeof(TestObserverActor<string>))]
        [InlineData(typeof(TestObserverAsyncActor<string>))]
        [InlineData(typeof(TestObserverExtendedActor<string>))]
        public async Task GetGenericNotification(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOf(() => new SubjectActor<string>()).Cast<SubjectRef<string>>();
            var a = ActorOf(Props.Create(actorType, subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(null);

            // Assert
            Assert.Equal(new[] { $"A", $"B,10" }, log);
        }
    }
}
