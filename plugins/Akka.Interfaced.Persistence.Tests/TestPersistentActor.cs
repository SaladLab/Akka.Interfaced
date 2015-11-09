using Akka.Interfaced.Persistence.Tests.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Xunit;
using System.Collections.Generic;

namespace Akka.Interfaced.Persistence.Tests
{
    public class TestPersistentActor : Akka.TestKit.Xunit2.TestKit
    {
        internal readonly CleanupLocalSnapshots Clean;

        public TestPersistentActor()
            : base (@"akka.persistence.snapshot-store.local.dir = ""temp_snapshots""")
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
