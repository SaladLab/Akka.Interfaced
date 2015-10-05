using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Akka.Interfaced.Tests
{
    public static class FilterLogBoard
    {
        private static ConcurrentQueue<string> _logs = new ConcurrentQueue<string>();

        public static void Log(string log)
        {
            _logs.Enqueue(log);
        }

        public static List<string> GetAndClearLogs()
        {
            var logs = _logs;
            _logs = new ConcurrentQueue<string>();
            return logs.ToList();
        }
    }
}
