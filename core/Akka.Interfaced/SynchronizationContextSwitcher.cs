using System;
using System.Threading;

namespace Akka.Interfaced
{
    internal struct SynchronizationContextSwitcher : IDisposable
    {
        private readonly SynchronizationContext _oldContext;

        public SynchronizationContextSwitcher(SynchronizationContext newContext)
        {
            _oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(newContext);
        }

        void IDisposable.Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_oldContext);
        }
    }
}
