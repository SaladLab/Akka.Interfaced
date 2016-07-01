using System;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit.Abstractions;
using Xunit;

namespace Akka.Interfaced
{
    public class RequestGenericTest : TestKit.Xunit2.TestKit
    {
        public RequestGenericTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestDummyActorBase : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestDummyActorBase(LogBoard<string> log)
            {
                _log = log;
            }

            protected T HandleCall<T>(T param)
            {
                _log.Add($"Call({param})");
                return param;
            }

            protected T HandleCall<T, U>(T param, U param2)
            {
                _log.Add($"Call({param}, {param2})");
                return param;
            }
        }

        public class TestDummyActor : TestDummyActorBase, IDummy<string>
        {
            public TestDummyActor(LogBoard<string> log)
                : base(log) { }

            Task<string> IDummy<string>.Call(string param) => Task.FromResult(HandleCall(param));
            Task<string> IDummy<string>.Call<U>(string param, U param2) => Task.FromResult(HandleCall(param, param2));
        }

        public class TestDummySyncActor : TestDummyActorBase, IDummySync<string>
        {
            public TestDummySyncActor(LogBoard<string> log)
                : base(log) { }

            string IDummySync<string>.Call(string param) => HandleCall(param);
            string IDummySync<string>.Call<U>(string param, U param2) => HandleCall(param, param2);
        }

        public class TestDummyExtendedActor : TestDummyActorBase, IExtendedInterface<IDummy<string>>
        {
            public TestDummyExtendedActor(LogBoard<string> log)
                : base(log) { }

            [ExtendedHandler]
            private string Call(string param) => HandleCall(param);

            [ExtendedHandler]
            private Task<string> Call<U>(string param, U param2) => Task.FromResult(HandleCall(param, param2));
        }

        public class TestDummyActor<T> : TestDummyActorBase, IDummy<T>
            where T : ICloneable
        {
            public TestDummyActor(LogBoard<string> log)
                : base(log) { }

            Task<T> IDummy<T>.Call(T param) => Task.FromResult(HandleCall(param));
            Task<T> IDummy<T>.Call<U>(T param, U param2) => Task.FromResult(HandleCall(param, param2));
        }

        public class TestDummySyncActor<T> : TestDummyActorBase, IDummySync<T>
            where T : ICloneable
        {
            public TestDummySyncActor(LogBoard<string> log)
                : base(log) { }

            T IDummySync<T>.Call(T param) => HandleCall(param);
            T IDummySync<T>.Call<U>(T param, U param2) => HandleCall(param, param2);
        }

        public class TestDummyExtendedActor<T> : TestDummyActorBase, IExtendedInterface<IDummy<T>>
            where T : ICloneable
        {
            public TestDummyExtendedActor(LogBoard<string> log)
                : base(log) { }

            [ExtendedHandler]
            private T Call(T param) => HandleCall(param);

            [ExtendedHandler]
            private Task<T> Call<U>(T param, U param2) => Task.FromResult(HandleCall(param, param2));
        }

        [Theory]
        [InlineData(typeof(TestDummyActor))]
        [InlineData(typeof(TestDummySyncActor))]
        [InlineData(typeof(TestDummyExtendedActor))]
        [InlineData(typeof(TestDummyActor<string>))]
        [InlineData(typeof(TestDummySyncActor<string>))]
        [InlineData(typeof(TestDummyExtendedActor<string>))]
        public async Task CallGenericMethod(Type actorType)
        {
            // Arrange
            var log = new LogBoard<string>();
            var a = ActorOf(Props.Create(actorType, log)).Cast<DummyRef<string>>();

            // Act
            var ret1 = await a.Call("A");
            var ret2 = await a.Call("B", 10);

            // Assert
            Assert.Equal("A", ret1);
            Assert.Equal("B", ret2);
            Assert.Equal(new[] { "Call(A)", "Call(B, 10)" }, log);
        }
    }
}
