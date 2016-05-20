using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Interfaced.Tests
{
    public class ExceptionActor : InterfacedActor, IWorker
    {
        private LogBoard _log;
        private string _tag;

        public ExceptionActor(LogBoard log, string tag = null)
        {
            _log = log;
            _tag = tag;
            _log.Log("ctor");
        }

        protected override void PreStart()
        {
            _log.Log("PreStart");

            if (_tag == "PreStart")
                throw new Exception();

            base.PreStart();
        }

        protected override async Task OnStart()
        {
            _log.Log("OnStart");

            if (_tag == "OnStart")
                throw new Exception();

            await Task.Yield();

            _log.Log("OnStart Done");

            if (_tag == "OnStart Done")
                throw new Exception();
        }

        protected override async Task OnGracefulStop()
        {
            _log.Log("OnGracefulStop");

            if (_tag == "OnGracefulStop")
                throw new Exception();

            await Task.Yield();

            _log.Log("OnGracefulStop Done");

            if (_tag == "OnGracefulStop Done")
                throw new Exception();
        }

        protected override void PostStop()
        {
            _log.Log("PostStop");

            if (_tag == "PostStop")
                throw new Exception();

            base.PostStop();
        }

        [MessageHandler]
        private void Handle(string message)
        {
            _log.Log($"Handle({message})");
            if (message == "E")
                throw new Exception();
        }

        async Task IWorker.Atomic(int id)
        {
            _log.Log($"Atomic({id})");

            if (id == -1)
                throw new Exception();
            else if (id == 1)
                throw new ArgumentException();

            await Task.Yield();

            if (id == -2)
                throw new Exception();
            else if (id == 2)
                throw new ArgumentException();

            _log.Log("Atomic({id}) Done");
        }

        [Reentrant]
        async Task IWorker.Reentrant(int id)
        {
            _log.Log($"Reentrant({id})");

            if (id == -1)
                throw new Exception();
            else if (id == 1)
                throw new ArgumentException();

            await Task.Yield();

            if (id == -2)
                throw new Exception();
            else if (id == 2)
                throw new ArgumentException();

            _log.Log("Reentrant({id}) Done");
        }
    }

    public class ExceptionHandle : Akka.TestKit.Xunit2.TestKit
    {
        public ExceptionHandle()
            : base("akka.actor.guardian-supervisor-strategy = \"Akka.Actor.StoppingSupervisorStrategy\"")
        {
        }

        [Fact]
        public void ExceptionThrown_At_PreStart()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, "PreStart")));

            actor.Tell("");

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                },
                log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_OnStart()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, "OnStart")));

            actor.Tell("");

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                    "OnStart",
                    "PostStop"
                },
                log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_OnStartDone()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, "OnStart Done")));

            actor.Tell("");

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                    "OnStart",
                    "OnStart Done",
                    "PostStop"
                },
                log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_MessageHandle()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, null)));

            actor.Tell("E");

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                    "OnStart",
                    "OnStart Done",
                    "Handle(E)",
                    "PostStop"
                },
                log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_OnGracefulStop()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, "OnGracefulStop")));

            actor.Tell(InterfacedPoisonPill.Instance);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                    "OnStart",
                    "OnStart Done",
                    "OnGracefulStop",
                    "PostStop"
                },
                log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_OnGracefulStopDone()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, "OnGracefulStop Done")));

            actor.Tell(InterfacedPoisonPill.Instance);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                    "OnStart",
                    "OnStart Done",
                    "OnGracefulStop",
                    "OnGracefulStop Done",
                    "PostStop"
                },
                log.GetAndClearLogs());
        }

        [Fact]
        public void ExceptionThrown_At_PostStop()
        {
            var log = new LogBoard();
            var actor = ActorOf(Props.Create(() => new ExceptionActor(log, "PostStop")));

            actor.Tell(InterfacedPoisonPill.Instance);

            Watch(actor);
            ExpectTerminated(actor);
            Assert.Equal(
                new[]
                {
                    "ctor",
                    "PreStart",
                    "OnStart",
                    "OnStart Done",
                    "OnGracefulStop",
                    "OnGracefulStop Done",
                    "PostStop"
                },
                log.GetAndClearLogs());
        }
    }
}
