using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

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
    }
}
