using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;

namespace Akka.Interfaced
{
    public class LogBoard<T> : IEnumerable<T>
    {
        private ConcurrentQueue<T> _logs = new ConcurrentQueue<T>();

        public void Add(T log)
        {
            _logs.Enqueue(log);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _logs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _logs.GetEnumerator();
        }

        public static LogBoard<T> GetLogBoard(object obj)
        {
            var field = obj.GetType().GetField("_log", BindingFlags.Instance | BindingFlags.NonPublic);
            return (LogBoard<T>)field?.GetValue(obj);
        }

        public static void Add(object obj, T log)
        {
            GetLogBoard(obj).Add(log);
        }
    }
}
