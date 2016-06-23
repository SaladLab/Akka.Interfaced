using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class NotificationDispatchTest : TestKit.Xunit2.TestKit
    {
        public NotificationDispatchTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestObserverActor : InterfacedActor, IDummy, ISubjectObserver
        {
            private SubjectRef _subject;
            private LogBoard<string> _log;

            public TestObserverActor(SubjectRef subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
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
                _log.Add(c + eventName);
            }
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task BasicActor_ObserveSubject(object context)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOf(() => new SubjectActor()).Cast<SubjectRef>();
            var a = ActorOf(() => new TestObserverActor(subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(context);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A" }, log);
        }

        public class TestObserverExtendedActor : InterfacedActor, IDummy, IExtendedInterface<ISubjectObserver>
        {
            private SubjectRef _subject;
            private LogBoard<string> _log;

            public TestObserverExtendedActor(SubjectRef subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
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
                _log.Add(c + eventName);
            }
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task ExtendedActor_ObserveSubject(object context)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOf(() => new SubjectActor()).Cast<SubjectRef>();
            var a = ActorOf(() => new TestObserverExtendedActor(subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(context);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A" }, log);
        }

        public class TestObserverExtendedAsyncActor : InterfacedActor, IDummy, IExtendedInterface<ISubjectObserver>
        {
            private SubjectRef _subject;
            private LogBoard<string> _log;

            public TestObserverExtendedAsyncActor(SubjectRef subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
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
                _log.Add(c + eventName + ":1");

                await Task.Delay(10);

                var contextMessage2 = ObserverContext != null ? ObserverContext + ":" : "";
                _log.Add(contextMessage2 + eventName + ":2");
            }
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task ExtendedAsyncActor_ObserveSubject(object context)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOf(() => new SubjectActor()).Cast<SubjectRef>();
            var a = ActorOf(() => new TestObserverExtendedAsyncActor(subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(context);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A:1", $"{c}A:2", $"{c}B:1", $"{c}B:2" }, log);
        }

        public class TestObserverExtendedAsyncReentrantActor : InterfacedActor, IDummy, IExtendedInterface<ISubjectObserver>
        {
            private SubjectRef _subject;
            private LogBoard<string> _log;

            public TestObserverExtendedAsyncReentrantActor(SubjectRef subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
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
                _log.Add(c + eventName + ":1");

                await Task.Delay(100);

                var contextMessage2 = ObserverContext != null ? ObserverContext + ":" : "";
                _log.Add(contextMessage2 + eventName + ":2");
            }
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task ExtendedAsyncReentrantActor_ObserveSubject(object context)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOf(() => new SubjectActor()).Cast<SubjectRef>();
            var a = ActorOf(() => new TestObserverExtendedAsyncReentrantActor(subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(context);
            await a.CastToIActorRef().GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A:1", $"{c}A:2" }, log.Where(x => x.StartsWith($"{c}A")));
            Assert.Equal(new[] { $"{c}B:1", $"{c}B:2" }, log.Where(x => x.StartsWith($"{c}B")));
        }

        public class TestObserverAsyncReentrantActor : InterfacedActor, IDummy, ISubjectObserverAsync
        {
            private SubjectRef _subject;
            private LogBoard<string> _log;

            public TestObserverAsyncReentrantActor(SubjectRef subject, LogBoard<string> log)
            {
                _subject = subject.WithRequestWaiter(this);
                _log = log;
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

            [Reentrant]
            async Task ISubjectObserverAsync.Event(string eventName)
            {
                var c = ObserverContext != null ? ObserverContext + ":" : "";
                _log.Add(c + eventName + ":1");

                await Task.Delay(100);

                var contextMessage2 = ObserverContext != null ? ObserverContext + ":" : "";
                _log.Add(contextMessage2 + eventName + ":2");
            }
        }

        [Theory, InlineData(null), InlineData("CTX")]
        public async Task AsyncReentrantActor_ObserveSubject(object context)
        {
            // Arrange
            var log = new LogBoard<string>();
            var subject = ActorOfAsTestActorRef(() => new SubjectActor()).Cast<SubjectRef>();
            var a = ActorOfAsTestActorRef(() => new TestObserverAsyncReentrantActor(subject, log)).Cast<DummyRef>();

            // Act
            await a.Call(context);
            await a.CastToIActorRef().GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            // Assert
            var c = context != null ? context + ":" : "";
            Assert.Equal(new[] { $"{c}A:1", $"{c}A:2" }, log.Where(x => x.StartsWith($"{c}A")));
            Assert.Equal(new[] { $"{c}B:1", $"{c}B:2" }, log.Where(x => x.StartsWith($"{c}B")));
        }
    }
}
