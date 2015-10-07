using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Akka.Interfaced.Tests
{
    public class FilterLogBoard
    {
        private ConcurrentQueue<string> _logs = new ConcurrentQueue<string>();

        public void Log(string log)
        {
            _logs.Enqueue(log);
        }

        public List<string> GetAndClearLogs()
        {
            var logs = _logs;
            _logs = new ConcurrentQueue<string>();
            return logs.ToList();
        }
    }
}
