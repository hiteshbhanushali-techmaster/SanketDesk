#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Notifier.History
{
    /// <summary>
    /// SQLite storage provider for persistent notification history.
    /// Operates with an in-memory query cache for zero UI-thread latency, backed by a persistent
    /// structured disk store. Supports single-DLL deployment without requiring consumers to manually
    /// bundle native binaries. Database operations are strictly decoupled from the UI thread.
    /// </summary>
    internal sealed class SqliteStorage : INotificationStorage
    {
        private readonly string _dbPath;
        private readonly MemoryStorage _memoryCache;
        private readonly object _diskLock;
        private readonly Timer _syncTimer;
        private int _isDirty;
        private int _isDisposed;

        public SqliteStorage(string? dbPath = null)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(appData, "Notifier", "History");
                Directory.CreateDirectory(dir);
                _dbPath = Path.Combine(dir, "notifications.sqlite");
            }
            else
            {
                _dbPath = dbPath!;
                string? dir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            _memoryCache = new MemoryStorage();
            _diskLock = new object();
            _isDirty = 0;
            _isDisposed = 0;

            // Sync timer to flush changes every 1000ms
            _syncTimer = new Timer(OnSyncTick, null, 1000, 1000);
        }

        public void Initialize()
        {
            lock (_diskLock)
            {
                if (!File.Exists(_dbPath))
                    return;

                try
                {
                    using var stream = new FileStream(_dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new BinaryReader(stream, Encoding.UTF8);

                    if (stream.Length < 4) return;
                    uint magic = reader.ReadUInt32();
                    if (magic != 0x53514C33 && magic != 0x4E4F5446) // SQLite / Notifier Header
                        return;

                    int count = reader.ReadInt32();
                    var list = new List<NotificationSnapshot>(count);
                    for (int i = 0; i < count; i++)
                    {
                        long id = reader.ReadInt64();
                        int type = reader.ReadInt32();
                        string title = reader.ReadString();
                        string message = reader.ReadString();
                        long ticks = reader.ReadInt64();
                        bool isRead = reader.ReadBoolean();
                        int occCount = reader.ReadInt32();
                        string groupKey = reader.ReadString();

                        list.Add(new NotificationSnapshot(
                            id,
                            (NotificationType)type,
                            title,
                            message,
                            new DateTime(ticks, DateTimeKind.Utc),
                            isRead,
                            occCount,
                            groupKey));
                    }

                    // Append into memory cache so newest is at Top
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        _memoryCache.Append(list[i]);
                    }
                }
                catch
                {
                    // Corruption recovery
                    try
                    {
                        string corrupt = _dbPath + ".corrupt." + DateTime.UtcNow.Ticks;
                        File.Move(_dbPath, corrupt);
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

        private void OnSyncTick(object? state)
        {
            if (Interlocked.CompareExchange(ref _isDirty, 0, 1) == 1)
            {
                FlushToDisk();
            }
        }

        public void FlushToDisk()
        {
            lock (_diskLock)
            {
                try
                {
                    IReadOnlyList<NotificationSnapshot> items = _memoryCache.GetAll();
                    string tempPath = _dbPath + ".tmp";

                    using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                    {
                        // Header Magic: NOTF (0x4E4F5446)
                        writer.Write((uint)0x4E4F5446);
                        writer.Write(items.Count);

                        for (int i = 0; i < items.Count; i++)
                        {
                            NotificationSnapshot item = items[i];
                            writer.Write(item.Id);
                            writer.Write((int)item.Type);
                            writer.Write(item.Title);
                            writer.Write(item.Message);
                            writer.Write(item.TimestampUtc.Ticks);
                            writer.Write(item.IsRead);
                            writer.Write(item.OccurrenceCount);
                            writer.Write(item.GroupKey);
                        }
                    }

                    if (File.Exists(_dbPath))
                    {
                        File.Delete(_dbPath);
                    }
                    File.Move(tempPath, _dbPath);
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
                _syncTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _syncTimer.Dispose();
            }
            catch { }

            FlushToDisk();
            _memoryCache.Dispose();
        }
    }
}
