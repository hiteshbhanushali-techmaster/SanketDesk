#nullable enable
using System;

namespace SanketDesk
{
    /// <summary>
    /// Represents an interactive action button within a notification card.
    /// </summary>
    public sealed class NotificationAction
    {
        /// <summary>
        /// Gets the display text for the action button (e.g. "Undo", "Review", "Dismiss").
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the optional callback invoked when the user clicks this action button.
        /// The callback receives the notification ID. If null, no action is taken.
        /// </summary>
        public Action<long>? Callback { get; }

        /// <summary>
        /// Gets optional custom metadata or state associated with this action.
        /// </summary>
        public object? Tag { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationAction"/> class.
        /// </summary>
        /// <param name="title">Action button title text.</param>
        /// <param name="callback">Optional click callback taking the notification ID.</param>
        /// <param name="tag">Optional custom tag.</param>
        public NotificationAction(string title, Action<long>? callback = null, object? tag = null)
        {
            Title = title ?? string.Empty;
            Callback = callback;
            Tag = tag;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
