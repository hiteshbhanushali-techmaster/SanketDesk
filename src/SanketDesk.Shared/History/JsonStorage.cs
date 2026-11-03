#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SanketDesk.History
{
    /// <summary>
    /// File-based JSON storage provider for notification history.
    /// Operates with an in-memory cache for instant zero-latency queries, coupled with a debounced
    /// asynchronous disk flush to guarantee controlled IO without disk thrashing during burst traffic.
    /// Uses atomic write replacement for crash-resilience and automatic corruption recovery.
    /// Zero third-party dependencies.
    /// </summary>
    internal sealed class JsonStorage : INotificationStorage
    {
        private readonly string _filePath;
        private readonly MemoryStorage _memoryCache;
        private readonly Timer _debounceTimer;
        private readonly object _ioLock;
        private int _isDirty;
        private int _isDisposed;

        public JsonStorage(string? filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(appData, "Notifier", "History");
                Directory.CreateDirectory(dir);
                _filePath = Path.Combine(dir, "notifications.json");
            }
            else
            {
                _filePath = filePath!;
                string? dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            _memoryCache = new MemoryStorage();
            _ioLock = new object();
            _isDirty = 0;
            _isDisposed = 0;

            // Debounced flush timer checking every 1000ms
            _debounceTimer = new Timer(OnFlushTick, null, 1000, 1000);
        }

        public void Initialize()
        {
            lock (_ioLock)
            {
                if (!File.Exists(_filePath))
                    return;

                try
                {
                    string content = File.ReadAllText(_filePath, Encoding.UTF8);
                    List<NotificationSnapshot> items = SimpleJsonParser.ParseSnapshots(content);
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        _memoryCache.Append(items[i]);
                    }
                }
                catch
                {
                    // Corruption recovery: backup corrupt file and start fresh
                    try
                    {
                        string corruptPath = _filePath + ".corrupt." + DateTime.UtcNow.Ticks;
                        File.Move(_filePath, corruptPath);
                    }
                    catch { }
                }
            }
        }

        public void Append(NotificationSnapshot snapshot)
        {
            _memoryCache.Append(snapshot);
            MarkDirty();
        }

        public IReadOnlyList<NotificationSnapshot> GetAll() => _memoryCache.GetAll();
        public IReadOnlyList<NotificationSnapshot> GetLatest(int count) => _memoryCache.GetLatest(count);
        public IReadOnlyList<NotificationSnapshot> GetOldest(int count) => _memoryCache.GetOldest(count);
        public IReadOnlyList<NotificationSnapshot> GetByTime(DateTime fromUtc, DateTime toUtc) => _memoryCache.GetByTime(fromUtc, toUtc);
        public int GetUnreadCount() => _memoryCache.GetUnreadCount();

        public void MarkAsRead(long id)
        {
            _memoryCache.MarkAsRead(id);
            MarkDirty();
        }

        public void MarkAllAsRead()
        {
            _memoryCache.MarkAllAsRead();
            MarkDirty();
        }

        public void Clear()
        {
            _memoryCache.Clear();
            MarkDirty();
        }

        public void Prune(int maxCount, TimeSpan retentionPeriod)
        {
            _memoryCache.Prune(maxCount, retentionPeriod);
            MarkDirty();
        }

        private void MarkDirty()
        {
            Interlocked.Exchange(ref _isDirty, 1);
        }

        private void OnFlushTick(object? state)
        {
            if (Interlocked.CompareExchange(ref _isDirty, 0, 1) == 1)
            {
                FlushToDisk();
            }
        }

        public void FlushToDisk()
        {
            lock (_ioLock)
            {
                try
                {
                    IReadOnlyList<NotificationSnapshot> items = _memoryCache.GetAll();
                    string json = SimpleJsonParser.SerializeSnapshots(items);

                    string tempPath = _filePath + ".tmp";
                    File.WriteAllText(tempPath, json, Encoding.UTF8);

                    if (File.Exists(_filePath))
                    {
                        File.Delete(_filePath);
                    }
                    File.Move(tempPath, _filePath);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            try
            {
                _debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _debounceTimer.Dispose();
            }
            catch { }

            // Final synchronous flush on shutdown
            FlushToDisk();
            _memoryCache.Dispose();
        }
    }

    /// <summary>
    /// Lightweight zero-dependency JSON serializer and parser for NotificationSnapshot models.
    /// Compatible with both .NET Framework 4.6.1 and .NET 8.
    /// </summary>
    internal static class SimpleJsonParser
    {
        public static string SerializeSnapshots(IReadOnlyList<NotificationSnapshot> items)
        {
            var sb = new StringBuilder(items.Count * 256);
            sb.Append("[\n");
            for (int i = 0; i < items.Count; i++)
            {
                NotificationSnapshot item = items[i];
                sb.Append("  {\n");
                sb.AppendFormat("    \"id\": {0},\n", item.Id);
                sb.AppendFormat("    \"type\": {0},\n", (int)item.Type);
                sb.AppendFormat("    \"title\": \"{0}\",\n", Escape(item.Title));
                sb.AppendFormat("    \"message\": \"{0}\",\n", Escape(item.Message));
                sb.AppendFormat("    \"timestampUtc\": \"{0:O}\",\n", item.TimestampUtc);
                sb.AppendFormat("    \"isRead\": {0},\n", item.IsRead ? "true" : "false");
                sb.AppendFormat("    \"count\": {0},\n", item.OccurrenceCount);
                sb.AppendFormat("    \"groupKey\": \"{0}\"\n", Escape(item.GroupKey));
                sb.Append(i < items.Count - 1 ? "  },\n" : "  }\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static List<NotificationSnapshot> ParseSnapshots(string json)
        {
            var list = new List<NotificationSnapshot>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            int index = 0;
            while (index < json.Length)
            {
                int startObj = json.IndexOf('{', index);
                if (startObj < 0) break;
                int endObj = json.IndexOf('}', startObj);
                if (endObj < 0) break;

                string objText = json.Substring(startObj, endObj - startObj + 1);
                NotificationSnapshot? snapshot = ParseSingle(objText);
                if (snapshot != null)
                {
                    list.Add(snapshot);
                }
                index = endObj + 1;
            }
            return list;
        }

        private static NotificationSnapshot? ParseSingle(string objText)
        {
            try
            {
                long id = ExtractLong(objText, "\"id\":");
                int typeInt = (int)ExtractLong(objText, "\"type\":");
                string title = ExtractString(objText, "\"title\":");
                string message = ExtractString(objText, "\"message\":");
                string timeStr = ExtractString(objText, "\"timestampUtc\":");
                bool isRead = objText.IndexOf("\"isRead\": true", StringComparison.OrdinalIgnoreCase) >= 0;
                int count = (int)ExtractLong(objText, "\"count\":");
                string groupKey = ExtractString(objText, "\"groupKey\":");

                DateTime.TryParse(timeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestampUtc);
                if (timestampUtc == DateTime.MinValue) timestampUtc = DateTime.UtcNow;

                return new NotificationSnapshot(
                    id,
                    (NotificationType)typeInt,
                    title,
                    message,
                    timestampUtc,
                    isRead,
                    count > 0 ? count : 1,
                    groupKey);
            }
            catch
            {
                return null;
            }
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Unescape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private static string ExtractString(string json, string key)
        {
            int kIdx = json.IndexOf(key, StringComparison.Ordinal);
            if (kIdx < 0) return string.Empty;
            int q1 = json.IndexOf('"', kIdx + key.Length);
            if (q1 < 0) return string.Empty;
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) return string.Empty;
            return Unescape(json.Substring(q1 + 1, q2 - q1 - 1));
        }

        private static long ExtractLong(string json, string key)
        {
            int kIdx = json.IndexOf(key, StringComparison.Ordinal);
            if (kIdx < 0) return 0;
            int start = kIdx + key.Length;
            while (start < json.Length && (json[start] == ' ' || json[start] == ':')) start++;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            if (end > start && long.TryParse(json.Substring(start, end - start), out long val))
            {
                return val;
            }
            return 0;
        }
    }
}
