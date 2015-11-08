using System;
using System.Net;
using Akka.Interfaced;
using System.Threading;
using System.Threading.Tasks;

#if NET20 || NET35

namespace SlimHttp.Program.Client
{
    class SlimTask : Task
    {
        private Exception _exception;

        internal HttpWebRequest WebRequest { get; set; }
        internal ManualResetEvent WaitEvent { get; set; }

        public object WaitHandle
        {
            get { return WaitEvent; }
        }

        public TaskStatus Status
        {
            get; internal set;
        }

        public Exception Exception
        {
            get { return _exception; }
            internal set
            {
                if (IsCompleted)
                    throw new InvalidOperationException("Already completed. status=" + Status);

                _exception = value;
                Status = TaskStatus.Faulted;
                WaitEvent.Set();
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Status == TaskStatus.RanToCompletion ||
                       Status == TaskStatus.Canceled ||
                       Status == TaskStatus.Faulted;
            }
        }
    }

    class SlimTask<TResult> : SlimTask, Task<TResult>
    {
        private TResult _result;

        public TResult Result
        {
            get
            {
                if (IsCompleted == false)
                    WaitEvent.WaitOne();

                if (Status == TaskStatus.RanToCompletion)
                    return _result;
                else if (Exception != null)
                    throw Exception;
                else
                    throw new InvalidOperationException();
            }
            internal set
            {
                if (IsCompleted)
                    throw new InvalidOperationException("Already completed. status=" + Status);

                _result = value;
                Status = TaskStatus.RanToCompletion;
                WaitEvent.Set();
            }
        }
    }

    static class SlimTaskExtension
    {
        public static void Wait(this Task task)
        {
            ((SlimTask)task).WaitEvent.WaitOne();
        }
    }
}

#endif
