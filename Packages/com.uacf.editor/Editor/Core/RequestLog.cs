using System.Collections.Generic;

namespace UACF.Core
{
    public static class RequestLog
    {
        private const int MaxEntries = 500;
        private static readonly Queue<LogEntry> _entries = new Queue<LogEntry>();
        private static readonly object _lock = new object();

        public static void Add(string action, bool ok, double durationSeconds, string errorCode = null)
        {
            lock (_lock)
            {
                _entries.Enqueue(new LogEntry
                {
                    Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Action = action,
                    Ok = ok,
                    Duration = durationSeconds,
                    ErrorCode = errorCode
                });
                while (_entries.Count > MaxEntries)
                    _entries.Dequeue();
            }
        }

        public static LogEntry[] GetLast(int count = 20)
        {
            lock (_lock)
            {
                var arr = _entries.ToArray();
                if (arr.Length <= count) return arr;
                var result = new LogEntry[count];
                System.Array.Copy(arr, arr.Length - count, result, 0, count);
                return result;
            }
        }

        public class LogEntry
        {
            public long Timestamp;
            public string Action;
            public bool Ok;
            public double Duration;
            public string ErrorCode;
        }
    }
}
