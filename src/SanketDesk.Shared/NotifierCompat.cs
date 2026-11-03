#nullable enable
using System;
using System.Drawing;

namespace SanketDesk
{
    /// <summary>
    /// Direct facade avoiding namespace ambiguity with the root <c>SanketDesk</c> namespace.
    /// Provides identical API to <see cref="SanketDesk"/>.
    /// </summary>
    public static class SanketDeskNotifier
    {
        public static bool IsInitialized => SanketDesk.IsInitialized;
        public static int UnreadCount => SanketDesk.UnreadCount;
        public static INotificationHistoryService History => SanketDesk.History;
        public static void Initialize(NotificationConfig config) => SanketDesk.Initialize(config);
        public static void Reinitialize(NotificationConfig config) => SanketDesk.Reinitialize(config);
        public static long Show(AdvancedNotificationOptions options) => SanketDesk.Show(options);
        public static long ShowSuccess(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowSuccess(title, message, icon, duration);
        public static long ShowInfo(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowInfo(title, message, icon, duration);
        public static long ShowWarning(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowWarning(title, message, icon, duration);
        public static long ShowError(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowError(title, message, icon, duration);
        public static void MarkAsRead(long notificationId) => SanketDesk.MarkAsRead(notificationId);
        public static void MarkAllAsRead() => SanketDesk.MarkAllAsRead();
        public static void ClearHistory() => SanketDesk.ClearHistory();
        public static void OpenNotificationCenter() => SanketDesk.OpenNotificationCenter();
        public static void CloseNotificationCenter() => SanketDesk.CloseNotificationCenter();
        public static void ToggleNotificationCenter() => SanketDesk.ToggleNotificationCenter();
        public static void SetFloatingIconVisible(bool visible) => SanketDesk.SetFloatingIconVisible(visible);
        public static void Dispose() => SanketDesk.Dispose();
    }

    /// <summary>
    /// Backward-compatibility facade for existing code referencing Notifier.
    /// Redirects all calls to <see cref="SanketDesk"/>.
    /// </summary>
    [Obsolete("Use SanketDesk or SanketDeskNotifier instead. Notifier is retained for backward compatibility.")]
    public static class Notifier
    {
        public static bool IsInitialized => SanketDesk.IsInitialized;
        public static int UnreadCount => SanketDesk.UnreadCount;
        public static INotificationHistoryService History => SanketDesk.History;
        public static void Initialize(NotificationConfig config) => SanketDesk.Initialize(config);
        public static void Reinitialize(NotificationConfig config) => SanketDesk.Reinitialize(config);
        public static long Show(AdvancedNotificationOptions options) => SanketDesk.Show(options);
        public static long ShowSuccess(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowSuccess(title, message, icon, duration);
        public static long ShowInfo(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowInfo(title, message, icon, duration);
        public static long ShowWarning(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowWarning(title, message, icon, duration);
        public static long ShowError(string title, string message, Image? icon = null, TimeSpan? duration = null) => SanketDesk.ShowError(title, message, icon, duration);
        public static void MarkAsRead(long notificationId) => SanketDesk.MarkAsRead(notificationId);
        public static void MarkAllAsRead() => SanketDesk.MarkAllAsRead();
        public static void ClearHistory() => SanketDesk.ClearHistory();
        public static void OpenNotificationCenter() => SanketDesk.OpenNotificationCenter();
        public static void CloseNotificationCenter() => SanketDesk.CloseNotificationCenter();
        public static void ToggleNotificationCenter() => SanketDesk.ToggleNotificationCenter();
        public static void SetFloatingIconVisible(bool visible) => SanketDesk.SetFloatingIconVisible(visible);
        public static void Dispose() => SanketDesk.Dispose();
    }
}
