#nullable enable
using System;
using System.Collections.Generic;

namespace Notifier
{
    /// <summary>
    /// Represents an immutable point-in-time snapshot of a notification for history retrieval and external consumption.
    /// Internal queue/history state cannot be corrupted through this DTO.
    /// </summary>
    public sealed class NotificationSnapshot
    {
        public long Id { get; }
        public NotificationType Type { get; }
        public string Title { get; }
        public string Message { get; }
        public DateTime TimestampUtc { get; }
        public bool IsRead { get; }
        public int OccurrenceCount { get; }
        public string GroupKey { get; }
        public IReadOnlyList<string> ActionNames { get; }
        public ToastDismissReason? DismissReason { get; }

        public NotificationSnapshot(
            long id,
            NotificationType type,
            string title,
            string message,
            DateTime timestampUtc,
            bool isRead,
            int occurrenceCount,
            string groupKey,
            IReadOnlyList<string>? actionNames = null,
            ToastDismissReason? dismissReason = null)
        {
            Id = id;
            Type = type;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            TimestampUtc = timestampUtc;
            IsRead = isRead;
            OccurrenceCount = occurrenceCount > 0 ? occurrenceCount : 1;
            GroupKey = groupKey ?? string.Empty;
            ActionNames = actionNames ?? Array.Empty<string>();
            DismissReason = dismissReason;
        }

        public override string ToString()
        {
            string countSuffix = OccurrenceCount > 1 ? $" ({OccurrenceCount})" : string.Empty;
            return $"[{Type}] #{Id} {Title}{countSuffix}: {Message}";
        }
    }
}
