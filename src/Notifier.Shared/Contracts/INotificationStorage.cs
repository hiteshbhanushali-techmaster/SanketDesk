using System;
using System.Collections.Generic;

namespace Notifier
{
    /// <summary>
    /// Internal persistence contract for notification history engines (Memory, JSON, SQLite).
    /// </summary>
    public interface INotificationStorage : IDisposable
    {
        void Initialize();
        void Append(NotificationSnapshot snapshot);
        IReadOnlyList<NotificationSnapshot> GetAll();
        IReadOnlyList<NotificationSnapshot> GetLatest(int count);
        IReadOnlyList<NotificationSnapshot> GetOldest(int count);
        IReadOnlyList<NotificationSnapshot> GetByTime(DateTime fromUtc, DateTime toUtc);
        int GetUnreadCount();
        void MarkAsRead(long id);
        void MarkAllAsRead();
        void Clear();
        void Prune(int maxCount, TimeSpan retentionPeriod);
    }
}
