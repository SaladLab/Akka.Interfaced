#if NET20 || NET35

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable SA1302 // Interface names must begin with I
#pragma warning disable SA1403 // File may only contain a single namespace

namespace Akka.Interfaced
{
    // Same with original TaskStatus
    public enum TaskStatus
    {
        Created,
        WaitingForActivation,
        WaitingToRun,
        Running,
        WaitingForChildrenToComplete,
        RanToCompletion,
        Canceled,
        Faulted
    }

    public interface Task
    {
        // Handle for waiting this task.
        // Depended on usage context (eg; Waiting coroutine in Unity)
        object WaitHandle { get; }

        // Same with original Task.Exception
        Exception Exception { get; }

        // Same with original Task.Status
        TaskStatus Status { get; }
    }

    public interface Task<TResult> : Task
    {
        // Same with original Task<TResult>.Result
        TResult Result { get; }
    }
}

namespace System.Threading.Tasks
{
    public static class PlaceholderClassForMakeNamespaceVisible
    {
    }
}

#endif
