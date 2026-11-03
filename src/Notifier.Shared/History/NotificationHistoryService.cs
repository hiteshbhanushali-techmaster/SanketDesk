#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace Notifier.History
{
    /// <summary>
    /// Thread-safe service for recording, querying, and managing notification history.
    /// Exposes immutable snapshots to prevent external mutation.
    /// Manages dual retention limits (MaxHistoryCount and MaxRetentionPeriod) via periodic background pruning.
    /// Read/unread state does NOT influence retention eviction.
    /// </summary>
    public sealed class NotificationHistoryService : INotificationHistoryService, IDisposable
    {
        private readonly NotificationConfig _config;
        private readonly INotificationStorage _storage;
        private readonly Timer _pruneTimer;
        private int _lastReportedUnreadCount;
        private int _isDisposed;

        public event Action<int>? UnreadCountChanged;
        public event Action<NotificationSnapshot>? NotificationAdded;

        public NotificationHistoryService(NotificationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            switch (config.StorageType)
            {
                case StorageType.Json:
                    _storage = new JsonStorage(config.StoragePath);
                    break;
                case StorageType.Sqlite:
                    _storage = new SqliteStorage(config.StoragePath);
                    break;
                case StorageType.Memory:
                default:
                    _storage = new MemoryStorage();
                    break;
            }

            _storage.Initialize();
            _lastReportedUnreadCount = _storage.GetUnreadCount();
            _isDisposed = 0;

            // Periodic prune every 30 seconds to enforce hard limits without per-notification overhead
            _pruneTimer = new Timer(OnPruneTick, null, 30000, 30000);
        }

        /// <summary>
        /// Records a new notification snapshot into history.
        /// Invoked by the engine upon notification dispatch.
        /// </summary>
        internal void Record(NotificationSnapshot snapshot)
        {
            if (Volatile.Read(ref _isDisposed) == 1)
                return;

            _storage.Append(snapshot);
            NotificationAdded?.Invoke(snapshot);

            CheckUnreadCountChanged();
        }

        public IReadOnlyList<NotificationSnapshot> GetAll() => _storage.GetAll();
        public IReadOnlyList<NotificationSnapshot> GetLatest(int count) => _storage.GetLatest(count);
        public IReadOnlyList<NotificationSnapshot> GetOldest(int count) => _storage.GetOldest(count);
        public IReadOnlyList<NotificationSnapshot> GetTop(int count) => _storage.GetLatest(count);
        public IReadOnlyList<NotificationSnapshot> GetBottom(int count) => _storage.GetOldest(count);
        public IReadOnlyList<NotificationSnapshot> GetByTime(DateTime fromUtc, DateTime toUtc) => _storage.GetByTime(fromUtc, toUtc);

        public int GetUnreadCount() => _storage.GetUnreadCount();

        public void MarkAsRead(long id)
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            _storage.MarkAsRead(id);
            CheckUnreadCountChanged();
        }

        public void MarkAllAsRead()
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            _storage.MarkAllAsRead();
            CheckUnreadCountChanged();
        }

        public void Clear()
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            _storage.Clear();
            CheckUnreadCountChanged();
        }

        private void CheckUnreadCountChanged()
        {
            int current = _storage.GetUnreadCount();
            int previous = Interlocked.Exchange(ref _lastReportedUnreadCount, current);
            if (previous != current)
            {
                UnreadCountChanged?.Invoke(current);
            }
        }

        private void OnPruneTick(object? state)
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            try
            {
                _storage.Prune(_config.MaxHistoryCount, _config.MaxRetentionPeriod);
                CheckUnreadCountChanged();
            }
            catch { }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            try
            {
                _pruneTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _pruneTimer.Dispose();
            }
            catch { }

            try
            {
                _storage.Prune(_config.MaxHistoryCount, _config.MaxRetentionPeriod);
                _storage.Dispose();
            }
            catch { }
        }
    }
}
