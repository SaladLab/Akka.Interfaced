using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    public class StartEventTest : TestKit.Xunit2.TestKit
    {
        public StartEventTest(ITestOutputHelper output)
            : base(output: output)
        {
        }

        public class TestStartActor : InterfacedActor
        {
            private LogBoard<string> _log;

            public TestStartActor(LogBoard<string> log)
            {
                _log = log;
            }

            protected override void PreStart()
            {
                _log.Add("PreStart()");
            }

            protected override void PostRestart(Exception cause)
            {
                _log.Add("PostRestart()");
            }

            protected override async Task OnStart(bool restarted)
            {
                _log.Add($"OnStart({restarted}) Begin");
                await Task.Delay(10);
                _log.Add($"OnStart({restarted}) End");
            }

            [MessageHandler]
            protected async Task Handle(string message)
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                _log.Add($"Handle({message}) Begin");
                await Task.Delay(10);
                _log.Add($"Handle({message}) End");
            }
        }

        [Fact]
        public async Task StartEvent_Order()
        {
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestStartActor(log)));

            actor.Tell("A");
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "PreStart()",
                    "OnStart(False) Begin",
                    "OnStart(False) End",
                    "Handle(A) Begin",
                    "Handle(A) End",
                },
                log);
        }

        [Fact]
        public async Task RestartEvent_Order()
        {
            var log = new LogBoard<string>();
            var actor = ActorOf(Props.Create(() => new TestStartActor(log)));

            actor.Tell(null);
            await actor.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "PreStart()",
                    "OnStart(False) Begin",
                    "OnStart(False) End",
                    // Exception occured.
                    "PostRestart()",
                    "OnStart(True) Begin",
                    "OnStart(True) End",
                },
                log);
        }
    }
}
