using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Akka.Interfaced
{
    public class LogBoard<T>
    {
        private ConcurrentQueue<T> _logs = new ConcurrentQueue<T>();

        public void Log(T log)
        {
            _logs.Enqueue(log);
        }

        public List<T> GetLogs()
        {
            return _logs.ToList();
        }
    }
}
