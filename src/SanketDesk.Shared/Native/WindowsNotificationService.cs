#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SanketDesk.Native
{
    /// <summary>
    /// Implements native Windows notification support for both .NET Framework 4.6.1 and .NET 8.
    /// On Windows 10/11, dispatches native desktop toast notifications.
    /// If WinRT is unavailable or the OS is not Windows 10+, gracefully degrades to Shell NotifyIcon balloons.
    /// Catches and logs all OS-level interop exceptions to ensure the host application NEVER crashes.
    /// </summary>
    internal sealed class WindowsNotificationService : IDisposable
    {
        private readonly NotificationConfig _config;
        private readonly IDiagnosticLogger _logger;
        private readonly bool _isWindows10OrGreater;
        private NotifyIcon? _fallbackTrayIcon;
        private readonly object _lock;
        private int _isDisposed;

        public WindowsNotificationService(NotificationConfig config, IDiagnosticLogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lock = new object();
            _isDisposed = 0;

            _isWindows10OrGreater = Environment.OSVersion.Platform == PlatformID.Win32NT &&
                                     Environment.OSVersion.Version.Major >= 10;
        }

        public void Show(NotificationModel model)
        {
            if (System.Threading.Volatile.Read(ref _isDisposed) == 1)
                return;

            try
            {
                if (_isWindows10OrGreater)
                {
                    bool success = TryShowWin10Toast(model);
                    if (success) return;
                }

                // Fallback to Shell Tray Balloon Notification
                ShowShellBalloon(model);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Windows native notification failed: {ex.Message}");
                // Never crash the host application
            }
        }

        private bool TryShowWin10Toast(NotificationModel model)
        {
            string aumid = !string.IsNullOrEmpty(_config.WindowsAppUserModelId)
                ? _config.WindowsAppUserModelId!
                : Process.GetCurrentProcess().ProcessName;

            try
            {
                // Escape XML characters
                string title = EscapeXml(model.GetFormattedTitle());
                string message = EscapeXml(model.Message);

                string toastXml = $@"<toast duration=""short"">
    <visual>
        <binding template=""ToastGeneric"">
            <text>{title}</text>
            <text>{message}</text>
        </binding>
    </visual>
</toast>";

                // Invoke WinRT ToastNotificationManager dynamically via COM / Reflection
                // to support both net461 and net8 without requiring Windows SDK NuGet package dependencies
                Type? xmlDocType = Type.GetType("Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom, ContentType=WindowsRuntime");
                Type? toastMgrType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType=WindowsRuntime");
                Type? toastType = Type.GetType("Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType=WindowsRuntime");

                if (xmlDocType != null && toastMgrType != null && toastType != null)
                {
                    object? xmlDoc = Activator.CreateInstance(xmlDocType);
                    var loadMethod = xmlDocType.GetMethod("LoadXml", new[] { typeof(string) });
                    loadMethod?.Invoke(xmlDoc, new object[] { toastXml });

                    object? toast = Activator.CreateInstance(toastType, xmlDoc);

                    var createNotifierMethod = toastMgrType.GetMethod("CreateToastNotifier", new[] { typeof(string) });
                    object? notifier = createNotifierMethod?.Invoke(null, new object[] { aumid });

                    if (notifier != null)
                    {
                        var showMethod = notifier.GetType().GetMethod("Show", new[] { toastType });
                        showMethod?.Invoke(notifier, new[] { toast });
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Win10 WinRT toast dispatch could not be completed: {ex.Message}");
            }

            return false;
        }

        private void ShowShellBalloon(NotificationModel model)
        {
            lock (_lock)
            {
                if (System.Threading.Volatile.Read(ref _isDisposed) == 1)
                    return;

                if (_fallbackTrayIcon == null)
                {
                    _fallbackTrayIcon = new NotifyIcon
                    {
                        Visible = true,
                        Icon = SystemIcons.Information
                    };
                }

                ToolTipIcon toolTipIcon = ToolTipIcon.Info;
                switch (model.Type)
                {
                    case NotificationType.Error:
                        toolTipIcon = ToolTipIcon.Error;
                        break;
                    case NotificationType.Warning:
                        toolTipIcon = ToolTipIcon.Warning;
                        break;
                    case NotificationType.Success:
                    case NotificationType.Info:
                    default:
                        toolTipIcon = ToolTipIcon.Info;
                        break;
                }

                string title = model.GetFormattedTitle();
                if (title.Length > 63) title = title.Substring(0, 60) + "...";

                string message = model.Message;
                if (message.Length > 255) message = message.Substring(0, 252) + "...";

                int durationMs = (int)model.DisplayDuration.TotalMilliseconds;
                if (durationMs <= 0) durationMs = 3000;

                _fallbackTrayIcon.ShowBalloonTip(durationMs, title, message, toolTipIcon);
            }
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            lock (_lock)
            {
                if (_fallbackTrayIcon != null)
                {
                    try
                    {
                        _fallbackTrayIcon.Visible = false;
                        _fallbackTrayIcon.Dispose();
                    }
                    catch { }
                    _fallbackTrayIcon = null;
                }
            }
        }
    }
}
