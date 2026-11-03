#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace Notifier
{
    /// <summary>
    /// Internal runtime model representing a notification instance as it travels through admission, scheduling, layout, and rendering.
    /// </summary>
    public sealed class NotificationModel
    {
        private int _occurrenceCount;

        public long Id { get; }
        public NotificationType Type { get; }
        public string Title { get; }
        public string Message { get; }
        public Image? Icon { get; }
        public string? CustomTypeBadge { get; }
        public string Template { get; }
        public NotificationPriority Priority { get; }
        public string GroupKey { get; }
        public DateTime CreatedAtUtc { get; }
        public DateTime ScheduledAtUtc { get; set; }
        public TimeSpan DisplayDuration { get; }
        public TimeSpan RemainingDuration { get; set; }
        public NotificationTarget Target { get; }
        public NotificationPosition Position { get; }
        public AnimationMode Animation { get; }
        public TextOverflowMode TextOverflow { get; }
        public IReadOnlyList<NotificationAction> Actions { get; }
        public Action<long>? OnClick { get; }
        public Action<long>? OnDoubleClick { get; }
        public Action<long>? OnClose { get; }
        public ToastDismissReason? DismissReason { get; set; }
        public bool IsRead { get; set; }
        public object? Tag { get; }

        public int OccurrenceCount => Volatile.Read(ref _occurrenceCount);

        public NotificationModel(
            long id,
            NotificationType type,
            string title,
            string message,
            Image? icon,
            string? customTypeBadge,
            string template,
            NotificationPriority priority,
            string groupKey,
            DateTime createdAtUtc,
            TimeSpan displayDuration,
            NotificationTarget target,
            NotificationPosition position,
            AnimationMode animation,
            TextOverflowMode textOverflow,
            IReadOnlyList<NotificationAction>? actions,
            Action<long>? onClick,
            Action<long>? onDoubleClick,
            Action<long>? onClose,
            object? tag = null)
        {
            Id = id;
            Type = type;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Icon = icon;
            CustomTypeBadge = customTypeBadge;
            Template = template;
            Priority = priority;
            GroupKey = groupKey ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            ScheduledAtUtc = createdAtUtc;
            DisplayDuration = displayDuration;
            RemainingDuration = displayDuration;
            Target = target;
            Position = position;
            Animation = animation;
            TextOverflow = textOverflow;
            Actions = actions ?? Array.Empty<NotificationAction>();
            OnClick = onClick;
            OnDoubleClick = onDoubleClick;
            OnClose = onClose;
            Tag = tag;
            _occurrenceCount = 1;
            IsRead = false;
        }

        public int IncrementOccurrence()
        {
            return Interlocked.Increment(ref _occurrenceCount);
        }

        public string GetFormattedTitle()
        {
            int count = OccurrenceCount;
            if (count > 1)
            {
                return $"{Title} ({count})";
            }
            return Title;
        }

        public NotificationSnapshot ToSnapshot()
        {
            List<string>? actionNames = null;
            if (Actions != null && Actions.Count > 0)
            {
                actionNames = new List<string>(Actions.Count);
                for (int i = 0; i < Actions.Count; i++)
                {
                    actionNames.Add(Actions[i].Title);
                }
            }

            return new NotificationSnapshot(
                Id,
                Type,
                Title,
                Message,
                CreatedAtUtc,
                IsRead,
                OccurrenceCount,
                GroupKey,
                actionNames,
                DismissReason);
        }
    }
}
