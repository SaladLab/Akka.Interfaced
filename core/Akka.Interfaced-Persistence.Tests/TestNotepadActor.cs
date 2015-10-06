using Akka.Interfaced;
using Akka.Interfaced.Persistence.Tests.Interface;
using System;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Persistence;

namespace Akka.Interfaced_Persistence.Tests
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

    public class TestNotepadActor : InterfacedPersistentActor<TestNotepadActor>, INotepad
    {
        private NotepadState _state;

        public override string PersistenceId => "Notepad";

        protected override Task OnPreStart()
        {
            _state = new NotepadState { Document = new List<string>() };
            return Task.FromResult(0);
        }

        #region Recover

        [MessageHandler]
        private void OnRecover(SnapshotOffer snapshot)
        {
            var state = (NotepadState)snapshot.Snapshot;
            if (state != null)
                _state = state;
        }

        [MessageHandler]
        private void OnRecover(NotepadClearEvent message)
        {
            _state.Document.Clear();
        }

        [MessageHandler]
        private void OnRecover(NotepadWriteEvent message)
        {
            _state.Document.Add(message.Message);
        }

        #endregion

        async Task INotepad.Clear()
        {
            await PersistTaskAsync(new NotepadClearEvent());
            _state.Document.Clear();
        }

        async Task INotepad.Write(string message)
        {
            await PersistTaskAsync(new NotepadClearEvent());
            _state.Document.Add(message);
        }

        Task<IList<string>> INotepad.GetDocument()
        {
            return Task.FromResult((IList<string>)_state.Document);
        }

        private Task PersistTaskAsync<TEvent>(TEvent @event)
        {
            var tcs = new TaskCompletionSource<bool>();
            Persist(@event, _ => tcs.SetResult(true));
            return tcs.Task;
        }
    }
}
