using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Interfaced.Persistence.Tests.Interface;
using Akka.Interfaced.Persistence;
using Akka.Persistence;

namespace Akka.Interfaced.Persistence.Tests
{
    public class NotepadClearEvent
    {
    }

    public class NotepadWriteEvent
    {
        public string Message;
    }

    public class NotepadState
    {
        public List<string> Document;
    }

    public class TestNotepadActor : InterfacedPersistentActor, INotepad
    {
        private List<string> _eventLog;
        private NotepadState _state;

        public override string PersistenceId { get; }

        public TestNotepadActor(string id, List<string> eventLog)
        {
            PersistenceId = id;
            _eventLog = eventLog;
        }

        protected override Task OnStart()
        {
            _state = new NotepadState { Document = new List<string>() };
            return Task.FromResult(0);
        }

        #region Recover

        [MessageHandler]
        private void OnRecover(SnapshotOffer snapshot)
        {
            _eventLog.Add("OnRecover(SnapshotOffer)");

            var state = (NotepadState)snapshot.Snapshot;
            if (state != null)
                _state = state;
        }

        [MessageHandler]
        private void OnRecover(NotepadClearEvent message)
        {
            _eventLog.Add("OnRecover(NotepadClearEvent)");

            _state.Document.Clear();
        }

        [MessageHandler]
        private void OnRecover(NotepadWriteEvent message)
        {
            _eventLog.Add("OnRecover(NotepadWriteEvent)");

            _state.Document.Add(message.Message);
        }

        #endregion

        #region INotepad

        async Task INotepad.Clear()
        {
            await PersistTaskAsync(new NotepadClearEvent());
            _state.Document.Clear();
        }

        async Task INotepad.Write(string message)
        {
            await PersistTaskAsync(new NotepadWriteEvent { Message = message });
            _state.Document.Add(message);
        }

        async Task INotepad.FlushSnapshot()
        {
            await SaveSnapshotTaskAsync(_state);
        }

        Task<IList<string>> INotepad.GetDocument()
        {
            return Task.FromResult((IList<string>)_state.Document);
        }

        #endregion
    }
}
