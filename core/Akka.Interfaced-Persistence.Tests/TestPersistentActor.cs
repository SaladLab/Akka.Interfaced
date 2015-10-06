using Akka.Interfaced.Persistence.Tests.Interface;
using System;
using System.Linq;
using Xunit;

namespace Akka.Interfaced_Persistence.Tests
{
    public class TestPersistentActor : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void TestMethod1()
        {
            var actor = ActorOfAsTestActorRef<TestNotepadActor>();
            var a = new NotepadRef(actor);
            a.Clear();
        }
    }
}
