#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Notifier
{
    /// <summary>
    /// Advanced configuration options for submitting fine-grained notifications.
    /// Used with Notifier.Show(AdvancedNotificationOptions).
    /// </summary>
    public sealed class AdvancedNotificationOptions
    {
        /// <summary>
        /// Notification type (Success, Info, Warning, Error, or Custom).
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.Info;

        /// <summary>
        /// Title text of the notification.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Body text / content of the notification.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional custom icon image. If null, the template's default icon for the NotificationType is rendered.
        /// </summary>
        public Image? Icon { get; set; }

        /// <summary>
        /// Custom badge or category label displayed when Type is Custom.
        /// </summary>
        public string? CustomTypeBadge { get; set; }

        /// <summary>
        /// Custom template name to render this notification (e.g. ModernCard, MinimalBar, SolidAccent, SplitPill, ActionPrompt, WindowsFluent).
        /// If null, the default template from initialization is used.
        /// </summary>
        public string? Template { get; set; }

        /// <summary>
        /// Explicit priority. If null or SystemDefault, resolved based on NotificationType.
        /// </summary>
        public NotificationPriority? Priority { get; set; }

        /// <summary>
        /// Deterministic grouping key used for burst clubbing and coalesce.
        /// If null, Title may be used as grouping key if grouping by Title is configured.
        /// </summary>
        public string? GroupKey { get; set; }

        /// <summary>
        /// Scheduling delay mode override for this notification.
        /// </summary>
        public NotificationDelayMode? DelayMode { get; set; }

        /// <summary>
        /// Scheduling delay duration for Fixed or Debounce delay modes.
        /// </summary>
        public TimeSpan? Delay { get; set; }

        /// <summary>
        /// Visible display duration on screen. Pauses when mouse hovers over toast.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Delivery target (InApp, Windows native toast/balloon, or Both).
        /// </summary>
        public NotificationTarget? Target { get; set; }

        /// <summary>
        /// Screen position override for this notification.
        /// </summary>
        public NotificationPosition? Position { get; set; }

        /// <summary>
        /// Visual appearance animation mode override.
        /// </summary>
        public AnimationMode? Animation { get; set; }

        /// <summary>
        /// Text overflow behavior override (Ellipsis or AutoHeight).
        /// </summary>
        public TextOverflowMode? TextOverflow { get; set; }

        /// <summary>
        /// Optional list of interactive action buttons rendered on the toast card.
        /// </summary>
        public IReadOnlyList<NotificationAction>? Actions { get; set; }

        /// <summary>
        /// Callback invoked when the user clicks the notification card body.
        /// </summary>
        public Action<long>? OnClick { get; set; }

        /// <summary>
        /// Callback invoked when the user double-clicks the notification card body.
        /// </summary>
        public Action<long>? OnDoubleClick { get; set; }

        /// <summary>
        /// Callback invoked when the notification is closed by the user or times out.
        /// </summary>
        public Action<long>? OnClose { get; set; }

        /// <summary>
        /// Optional user-defined tag or state object.
        /// </summary>
        public object? Tag { get; set; }
    }
}
