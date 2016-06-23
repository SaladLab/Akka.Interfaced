using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class NotificationInheritanceTest : TestKit.Xunit2.TestKit
    {
        public NotificationInheritanceTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestObserverBaseActor : InterfacedActor, IDummy
        {
            private SubjectExRef _subject;
            protected LogBoard<string> _log;

            public TestObserverBaseActor(SubjectExRef subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
            }

            [Reentrant]
            async Task<object> IDummy.Call(object param)
            {
                var observer = CreateObserver<ISubjectExObserver>(param);
                await _subject.Subscribe(observer);
                await _subject.MakeEvent("A");
                await _subject.MakeEventEx("B");
                await _subject.Unsubscribe(observer);
                RemoveObserver(observer);
                return null;
            }
        }

        public class TestObserverActor : TestObserverBaseActor, ISubjectExObserver
        {
            public TestObserverActor(SubjectExRef subject, LogBoard<string> log)
                : base(subject, log)
            {
            }

            void ISubjectObserver.Event(string eventName)
            {
                _log.Add("Event:" + eventName);
            }

            void ISubjectExObserver.EventEx(string eventName)
            {
                _log.Add("EventEx:" + eventName);
            }
        }

        public class TestObserverAsyncActor : TestObserverBaseActor, ISubjectExObserverAsync
        {
            public TestObserverAsyncActor(SubjectExRef subject, LogBoard<string> log)
                : base(subject, log)
            {
            }

            Task ISubjectObserverAsync.Event(string eventName)
            {
                _log.Add("Event:" + eventName);
                return Task.FromResult(true);
            }

            Task ISubjectExObserverAsync.EventEx(string eventName)
            {
                _log.Add("EventEx:" + eventName);
                return Task.FromResult(true);
            }
        }

        public class TestObserverExtendedActor : TestObserverBaseActor, IExtendedInterface<ISubjectExObserver>
        {
            public TestObserverExtendedActor(SubjectExRef subject, LogBoard<string> log)
                : base(subject, log)
            {
            }

            [ExtendedHandler]
            private void Event(string eventName)
            {
                _log.Add("Event:" + eventName);
            }

            [ExtendedHandler]
            private Task EventEx(string eventName)
            {
                _log.Add("EventEx:" + eventName);
                return Task.FromResult(true);
            }
        }

        [Theory]
        [InlineData(typeof(TestObserverActor))]
        [InlineData(typeof(TestObserverAsyncActor))]
        [InlineData(typeof(TestObserverExtendedActor))]
        public async Task BasicActor_ObserveSubject(object context)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOf(() => new SubjectExActor()).Cast<SubjectExRef>();
            var a = ActorOf(() => new TestObserverActor(subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(context);

            // Assert
            Assert.Equal(new[] { "Event:A", "Event:B" }, log);
        }
    }
}
