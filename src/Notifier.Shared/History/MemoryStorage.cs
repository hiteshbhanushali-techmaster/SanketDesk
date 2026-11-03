#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace Notifier.History
{
    /// <summary>
    /// High-performance thread-safe in-memory storage implementation for notification history.
    /// Orders notifications newest-first (Top = newest, Bottom = oldest).
    /// Enforces hard limits on MaxHistoryCount (FIFO eviction) and MaxRetentionPeriod.
    /// Read/unread state does NOT influence eviction priority.
    /// </summary>
    internal sealed class MemoryStorage : INotificationStorage
    {
        private readonly List<NotificationSnapshot> _items;
        private readonly ReaderWriterLockSlim _lock;
        private int _unreadCount;

        public MemoryStorage()
        {
            _items = new List<NotificationSnapshot>(512);
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _unreadCount = 0;
        }

        public void Initialize()
        {
            // In-memory requires no disk initialization
        }

        public void Append(NotificationSnapshot snapshot)
        {
            if (snapshot == null) return;

            _lock.EnterWriteLock();
            try
            {
                // Insert at beginning so index 0 is always newest (Top)
                _items.Insert(0, snapshot);
                if (!snapshot.IsRead)
                {
                    _unreadCount++;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IReadOnlyList<NotificationSnapshot> GetAll()
        {
            _lock.EnterReadLock();
            try
            {
                return _items.ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<NotificationSnapshot> GetLatest(int count)
        {
            if (count <= 0) return Array.Empty<NotificationSnapshot>();

            _lock.EnterReadLock();
            try
            {
                int take = Math.Min(count, _items.Count);
                var result = new NotificationSnapshot[take];
                _items.CopyTo(0, result, 0, take);
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<NotificationSnapshot> GetOldest(int count)
        {
            if (count <= 0) return Array.Empty<NotificationSnapshot>();

            _lock.EnterReadLock();
            try
            {
                int take = Math.Min(count, _items.Count);
                var result = new NotificationSnapshot[take];
                int start = _items.Count - take;
                // Oldest are at the end of the list; return in oldest-first sequence
                for (int i = 0; i < take; i++)
                {
                    result[i] = _items[_items.Count - 1 - i];
                }
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<NotificationSnapshot> GetByTime(DateTime fromUtc, DateTime toUtc)
        {
            _lock.EnterReadLock();
            try
            {
                var result = new List<NotificationSnapshot>();
                for (int i = 0; i < _items.Count; i++)
                {
                    NotificationSnapshot item = _items[i];
                    if (item.TimestampUtc >= fromUtc && item.TimestampUtc <= toUtc)
                    {
                        result.Add(item);
                    }
                }
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int GetUnreadCount()
        {
            _lock.EnterReadLock();
            try
            {
                return _unreadCount;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void MarkAsRead(long id)
        {
            _lock.EnterWriteLock();
            try
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    NotificationSnapshot item = _items[i];
                    if (item.Id == id)
                    {
                        if (!item.IsRead)
                        {
                            _items[i] = new NotificationSnapshot(
                                item.Id,
                                item.Type,
                                item.Title,
                                item.Message,
                                item.TimestampUtc,
                                true, // IsRead = true
                                item.OccurrenceCount,
                                item.GroupKey,
                                item.ActionNames,
                                item.DismissReason);

                            _unreadCount = Math.Max(0, _unreadCount - 1);
                        }
                        break;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void MarkAllAsRead()
        {
            _lock.EnterWriteLock();
            try
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    NotificationSnapshot item = _items[i];
                    if (!item.IsRead)
                    {
                        _items[i] = new NotificationSnapshot(
                            item.Id,
                            item.Type,
                            item.Title,
                            item.Message,
                            item.TimestampUtc,
                            true,
                            item.OccurrenceCount,
                            item.GroupKey,
                            item.ActionNames,
                            item.DismissReason);
                    }
                }
                _unreadCount = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _items.Clear();
                _unreadCount = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Applies hard retention limits:
        /// 1. Evicts items older than retentionPeriod.
        /// 2. If count > maxCount, trims oldest items from the bottom (FIFO).
        /// Read/unread state does NOT influence eviction priority.
        /// </summary>
        public void Prune(int maxCount, TimeSpan retentionPeriod)
        {
            if (maxCount < 1) maxCount = 1;

            _lock.EnterWriteLock();
            try
            {
                DateTime cutoffUtc = DateTime.UtcNow.Subtract(retentionPeriod);

                // 1. Prune by retention period (oldest items are at the end)
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    if (_items[i].TimestampUtc < cutoffUtc)
                    {
                        if (!_items[i].IsRead)
                        {
                            _unreadCount = Math.Max(0, _unreadCount - 1);
                        }
                        _items.RemoveAt(i);
                    }
                }

                // 2. Prune by maximum count (remove oldest from end of list)
                while (_items.Count > maxCount)
                {
                    int lastIndex = _items.Count - 1;
                    if (!_items[lastIndex].IsRead)
                    {
                        _unreadCount = Math.Max(0, _unreadCount - 1);
                    }
                    _items.RemoveAt(lastIndex);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _lock.EnterWriteLock();
            try
            {
                _items.Clear();
                _unreadCount = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            _lock.Dispose();
        }
    }
}
