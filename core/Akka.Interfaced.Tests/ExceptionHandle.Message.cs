using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;

namespace Akka.Interfaced.Tests
{
    public class ExceptionActor_Message : InterfacedActor
    {
        private LogBoard _log;

        public ExceptionActor_Message(LogBoard log)
        {
            _log = log;
        }

        [MessageHandler]
        private void Handle(string message)
        {
            _log.Log($"Handle({message})");
            if (message == "E")
                throw new Exception();
        }

        [MessageHandler]
        private async Task HandleAsync(int message)
        {
            _log.Log($"HandleAsync({message})");

            if (message == 1)
                throw new Exception();

            await Task.Yield();

            _log.Log($"HandleAsync({message}) Done");

            if (message == 2)
                throw new Exception();
        }

        [MessageHandler, Reentrant]
        private async Task HandleReentrantAsync(long message)
        {
            _log.Log($"HandleReentrantAsync({message})");

            if (message == 1)
                throw new Exception();

            await Task.Yield();

            _log.Log($"HandleReentrantAsync({message}) Done");

            if (message == 2)
                throw new Exception();
        }
    }

    public class ExceptionHandle_Message : Akka.TestKit.Xunit2.TestKit
    {
        public ExceptionHandle_Message()
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"")
        {
        }

        [Fact]
        public void ExceptionThrown_At_Handle()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor_Message(log)));

            actor.Tell("E");

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "Handle(E)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_HandleAsync()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor_Message(log)));

            actor.Tell(1);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleAsync(1)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_HandleAsyncDone()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor_Message(log)));

            actor.Tell(2);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleAsync(2)", "HandleAsync(2) Done" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_HandleReentrantAsync()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor_Message(log)));

            actor.Tell(1L);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleReentrantAsync(1)" },
                         log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_HandleReentrantAsyncDone()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor_Message(log)));

            actor.Tell(2L);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(new[] { "HandleReentrantAsync(2)", "HandleReentrantAsync(2) Done" },
                         log.GetAndClearLogs());
        }
    }
}
