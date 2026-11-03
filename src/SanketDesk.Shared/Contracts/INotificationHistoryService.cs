using System;
using System.Collections.Generic;

namespace SanketDesk
{
    /// <summary>
    /// Service contract for querying and managing notification history.
    /// Exposes immutable snapshots to prevent external mutation of internal engine state.
    /// </summary>
    public interface INotificationHistoryService
    {
        /// <summary>
        /// Gets all stored notification snapshots in newest-first order.
        /// </summary>
        IReadOnlyList<NotificationSnapshot> GetAll();

        /// <summary>
        /// Gets notification snapshots created within the specified UTC time range.
        /// </summary>
        IReadOnlyList<NotificationSnapshot> GetByTime(DateTime fromUtc, DateTime toUtc);

        /// <summary>
        /// Gets the newest stored notifications up to the specified count (Top / Newest first).
        /// </summary>
        IReadOnlyList<NotificationSnapshot> GetLatest(int count);

        /// <summary>
        /// Gets the oldest stored notifications up to the specified count (Bottom / Oldest first).
        /// </summary>
        IReadOnlyList<NotificationSnapshot> GetOldest(int count);

        /// <summary>
        /// Alias for <see cref="GetLatest(int)"/> (newest first).
        /// </summary>
        IReadOnlyList<NotificationSnapshot> GetTop(int count);

        /// <summary>
        /// Alias for <see cref="GetOldest(int)"/> (oldest first).
        /// </summary>
        IReadOnlyList<NotificationSnapshot> GetBottom(int count);

        /// <summary>
        /// Gets the current number of unread notifications in history.
        /// </summary>
        int GetUnreadCount();

        /// <summary>
        /// Marks the notification with the specified ID as read.
        /// </summary>
        void MarkAsRead(long id);

        /// <summary>
        /// Marks all stored notifications as read.
        /// </summary>
        void MarkAllAsRead();

        /// <summary>
        /// Clears all notification history records.
        /// </summary>
        void Clear();

        /// <summary>
        /// Fired when the unread count changes.
        /// </summary>
        event Action<int> UnreadCountChanged;

        /// <summary>
        /// Fired when a new notification snapshot is added to history.
        /// </summary>
        event Action<NotificationSnapshot> NotificationAdded;
    }
}
