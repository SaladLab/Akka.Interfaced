using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Xunit;
using Akka.Configuration;
using Xunit.Abstractions;

namespace Akka.Interfaced.Persistence.Tests
{
    public class TestPersistentActor : Akka.TestKit.Xunit2.TestKit
    {
        internal readonly CleanupLocalSnapshots Clean;

        public TestPersistentActor(ITestOutputHelper output)
            : base(ConfigurationFactory.ParseString(
                    @"akka.persistence.journal.plugin = ""akka.persistence.journal.inmem""
                      akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.local""
                      akka.persistence.snapshot-store.local.dir = ""temp_snapshots/""")
                    .WithFallback(Akka.Persistence.Persistence.DefaultConfig()),
                   output: output)
        {
            Clean = new CleanupLocalSnapshots(this);
            Clean.Initialize();
        }

        protected override void AfterAll()
        {
            base.AfterAll();
            Clean.Dispose();
        }

        [Fact]
        public async Task Test_PersistentActor_Work()
        {
            // create
            {
                var eventLog = new List<string>();
                var actor = ActorOfAsTestActorRef<TestNotepadActor>(Props.Create<TestNotepadActor>("notepad1", eventLog));
                var a = new NotepadRef(actor);
                await a.Clear();
                await a.Write("Apple");
                await a.Write("Banana");
                await a.FlushSnapshot();
                await a.Write("Cinamon");
                var doc = await a.GetDocument();
                await a.Actor.GracefulStop(TimeSpan.FromSeconds(5), InterfacedPoisonPill.Instance);

                Assert.Equal(new List<string> { "Apple", "Banana", "Cinamon" }, doc);
                Assert.Equal(new List<string>(), eventLog);
            }

            // incarnation from snapshot & journal
            {
                var eventLog = new List<string>();
                var actor = ActorOfAsTestActorRef<TestNotepadActor>(Props.Create<TestNotepadActor>("notepad1", eventLog));
                var a = new NotepadRef(actor);
                var doc = await a.GetDocument();

                Assert.Equal(new List<string> { "Apple", "Banana", "Cinamon" }, doc);
                Assert.Equal(new List<string>
                {
                    "OnRecover(SnapshotOffer)",
                    "OnRecover(NotepadWriteEvent)"
                }, eventLog);
            }
        }
    }
}
