using System;

namespace SanketDesk
{
    /// <summary>
    /// Represents the built-in and extensible notification types.
    /// Basic API supports Success, Info, Warning, Error.
    /// Advanced API additionally supports Custom.
    /// </summary>
    public enum NotificationType
    {
        Success = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Custom = 4
    }

    /// <summary>
    /// Explicit priority values for individual notifications.
    /// </summary>
    public enum NotificationPriority
    {
        SystemDefault = -1,
        Success = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Priority evaluation mode across the notification engine.
    /// SystemPriority: Error > Warning > Info > Success.
    /// EqualPriority: Strict FIFO.
    /// </summary>
    public enum PriorityMode
    {
        SystemPriority = 0,
        EqualPriority = 1
    }

    /// <summary>
    /// Defines admission and queueing behavior under high-frequency burst submissions.
    /// </summary>
    public enum BurstStrategy
    {
        /// <summary>
        /// Enqueue items up to maximum queue capacity.
        /// </summary>
        Queue = 0,

        /// <summary>
        /// Drop excess items immediately at admission once queue capacity is reached.
        /// </summary>
        Drop = 1,

        /// <summary>
        /// Coalesce pending notifications into an aggregate summary.
        /// </summary>
        Coalesce = 2,

        /// <summary>
        /// Atomically update pending items matching GroupKey or Title in-place (Title (N)) without allocating new queue nodes.
        /// </summary>
        CoalesceByKey = 3
    }

    /// <summary>
    /// Configures the deterministic grouping and clubbing strategy.
    /// </summary>
    public enum NotificationGroupingStrategy
    {
        /// <summary>
        /// Group notifications that share the same explicit GroupKey.
        /// </summary>
        ByGroupKey = 0,

        /// <summary>
        /// Group notifications that share the exact same Title text.
        /// </summary>
        ByTitle = 1,

        /// <summary>
        /// Group by explicit GroupKey when provided; otherwise group by Title.
        /// </summary>
        ByKeyOrTitle = 2
    }

    /// <summary>
    /// Defines scheduling delay and debounce behavior.
    /// </summary>
    public enum NotificationDelayMode
    {
        /// <summary>
        /// Immediate scheduling without delay.
        /// </summary>
        None = 0,

        /// <summary>
        /// Waits for the configured fixed delay interval before scheduling.
        /// </summary>
        Fixed = 1,

        /// <summary>
        /// Resets the scheduling window if repeated notifications with the same key arrive within the interval.
        /// </summary>
        Debounce = 2,

        /// <summary>
        /// Accumulates notifications arriving within the interval and dispatches them together.
        /// </summary>
        Batch = 3
    }

    /// <summary>
    /// High-level screen anchor positions for toast notifications.
    /// </summary>
    public enum NotificationPosition
    {
        Top = 0,
        Bottom = 1,
        TopLeft = 2,
        TopCenter = 3,
        TopRight = 4,
        BottomLeft = 5,
        BottomCenter = 6,
        BottomRight = 7,
        Center = 8
    }

    /// <summary>
    /// Delivery targets for notifications.
    /// </summary>
    public enum NotificationTarget
    {
        /// <summary>
        /// Display via the in-app WinForms toast system only.
        /// </summary>
        InApp = 0,

        /// <summary>
        /// Display via the Windows native notification system only (WinRT toast or shell tray balloon fallback).
        /// </summary>
        Windows = 1,

        /// <summary>
        /// Display both in-app and native Windows notifications.
        /// </summary>
        Both = 2
    }

    /// <summary>
    /// Text overflow layout behavior within notification cards.
    /// </summary>
    public enum TextOverflowMode
    {
        /// <summary>
        /// Truncate long lines with an ellipsis (...).
        /// </summary>
        Ellipsis = 0,

        /// <summary>
        /// Expand notification card height to fit content within internal safety limits.
        /// </summary>
        AutoHeight = 1
    }

    /// <summary>
    /// Storage provider types for notification history.
    /// </summary>
    public enum StorageType
    {
        Memory = 0,
        Json = 1,
        Sqlite = 2
    }

    /// <summary>
    /// Visual transition animations for toast appearance and retirement.
    /// </summary>
    public enum AnimationMode
    {
        None = 0,
        Fade = 1,
        Slide = 2,
        FadeAndSlide = 3
    }

    /// <summary>
    /// Dismissal reason for active notifications.
    /// </summary>
    public enum ToastDismissReason
    {
        Timeout = 0,
        UserClose = 1,
        UserClick = 2,
        ActionClicked = 3,
        Evicted = 4,
        Disposed = 5
    }

    /// <summary>
    /// Hit-test areas on a notification toast for mouse interactions.
    /// </summary>
    public enum NotificationHitArea
    {
        None = 0,
        Body = 1,
        CloseButton = 2,
        ActionButton = 3
    }
}
