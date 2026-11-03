#nullable enable
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SanketDesk.Engine;
using SanketDesk.History;
using SanketDesk.Native;
using SanketDesk.Templates;
using SanketDesk.UI;

namespace SanketDesk
{
    /// <summary>
    /// Master public entry point and facade for the SanketDesk library.
    /// Thread-safe across thousands of simultaneous producer threads.
    /// Manages the full lifecycle of notification ingestion, scheduling, toasts, history,
    /// notification center, and floating action icon.
    /// </summary>
    public static class SanketDesk
    {
        private static readonly object _initLock = new object();
        private static NotificationConfig? _config;
        private static NotificationEngine? _engine;
        private static NotificationHistoryService? _historyService;
        private static ActiveToastManager? _toastManager;
        private static WindowsNotificationService? _windowsNotificationService;
        private static FloatingIconForm? _floatingIconForm;
        private static NotificationCenterForm? _notificationCenterForm;
        private static IUiDispatcher? _uiDispatcher;
        private static int _isInitialized;
        private static int _isDisposed;

        /// <summary>
        /// Gets whether the Notifier has been initialized.
        /// </summary>
        public static bool IsInitialized => Volatile.Read(ref _isInitialized) == 1;

        /// <summary>
        /// Gets the current unread notification count.
        /// Returns 0 if history is disabled or not initialized.
        /// </summary>
        public static int UnreadCount => _historyService?.GetUnreadCount() ?? 0;

        /// <summary>
        /// Gets the notification history service for querying snapshots and managing read states.
        /// </summary>
        public static INotificationHistoryService History
        {
            get
            {
                AssertInitialized();
                return _historyService!;
            }
        }

        /// <summary>
        /// Initializes the Notifier engine with the specified master configuration.
        /// Configuration is frozen upon initialization to guarantee thread-safety.
        /// Only one Notifier instance is permitted per application/process.
        /// </summary>
        /// <param name="config">Master configuration.</param>
        public static void Initialize(NotificationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            lock (_initLock)
            {
                if (Volatile.Read(ref _isInitialized) == 1)
                {
                    throw new InvalidOperationException("Notifier has already been initialized. Only one Notifier manager is supported per process.");
                }

                config.Validate();
                config.Freeze();
                _config = config;

                _uiDispatcher = new UiDispatcher(config.HostForm);
                var clock = SystemClock.Instance;
                var logger = NullDiagnosticLogger.Instance;

                // 1. History & Persistence
                _historyService = new NotificationHistoryService(config);

                // 2. Windows Native Notifications
                _windowsNotificationService = new WindowsNotificationService(config, logger);

                // 3. WinForms Active Toast Manager
                _toastManager = new ActiveToastManager(
                    config,
                    _uiDispatcher,
                    templateName => TemplateRegistry.Instance.Resolve(templateName));

                // 4. Core Ingestion Engine
                _engine = new NotificationEngine(
                    config,
                    clock,
                    logger,
                    batch => _toastManager.EnqueueBatchForDisplay(batch),
                    item => _windowsNotificationService.Show(item),
                    snapshot => _historyService.Record(snapshot));

                // 5. Notification Center Window
                _notificationCenterForm = new NotificationCenterForm(_historyService, config);

                // 6. Floating Action Icon (if enabled)
                if (config.EnableFloatingIcon && config.HostForm != null)
                {
                    _floatingIconForm = new FloatingIconForm(
                        config.HostForm,
                        config,
                        _historyService,
                        ToggleNotificationCenter);
                    _floatingIconForm.Show();
                }

                Interlocked.Exchange(ref _isDisposed, 0);
                Interlocked.Exchange(ref _isInitialized, 1);
            }
        }

        #region Basic Notification API (Simple, Thread-Safe, Zero Boilerplate)

        /// <summary>
        /// Shows a Success toast notification.
        /// </summary>
        public static long ShowSuccess(string title, string message, Image? icon = null, TimeSpan? duration = null)
        {
            return SubmitBasic(NotificationType.Success, title, message, icon, duration);
        }

        /// <summary>
        /// Shows an Info toast notification.
        /// </summary>
        public static long ShowInfo(string title, string message, Image? icon = null, TimeSpan? duration = null)
        {
            return SubmitBasic(NotificationType.Info, title, message, icon, duration);
        }

        /// <summary>
        /// Shows a Warning toast notification.
        /// </summary>
        public static long ShowWarning(string title, string message, Image? icon = null, TimeSpan? duration = null)
        {
            return SubmitBasic(NotificationType.Warning, title, message, icon, duration);
        }

        /// <summary>
        /// Shows an Error toast notification.
        /// </summary>
        public static long ShowError(string title, string message, Image? icon = null, TimeSpan? duration = null)
        {
            return SubmitBasic(NotificationType.Error, title, message, icon, duration);
        }

        private static long SubmitBasic(NotificationType type, string title, string message, Image? icon, TimeSpan? duration)
        {
            AssertInitialized();

            TimeSpan displayDuration = duration ?? _config!.DefaultDisplayDuration;
            long id = _engine!.NextNotificationId();

            var model = new NotificationModel(
                id,
                type,
                title,
                message,
                icon,
                null,
                _config!.DefaultTemplate,
                (NotificationPriority)(int)type,
                string.Empty,
                DateTime.UtcNow,
                displayDuration,
                _config.Target,
                _config.Position,
                _config.AnimationMode,
                _config.TextOverflowMode,
                null,
                null,
                null,
                null);

            return _engine.Submit(model);
        }

        #endregion

        #region Advanced Notification API

        /// <summary>
        /// Submits an advanced notification configured with custom templates, priorities, grouping keys,
        /// delays, action buttons, custom type badges, or callbacks.
        /// </summary>
        public static long Show(AdvancedNotificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            AssertInitialized();

            TimeSpan displayDuration = options.Duration ?? _config!.DefaultDisplayDuration;
            NotificationPriority priority = options.Priority ?? (NotificationPriority)(int)options.Type;
            string template = !string.IsNullOrEmpty(options.Template) ? options.Template! : _config!.DefaultTemplate;
            NotificationTarget target = options.Target ?? _config!.Target;
            NotificationPosition position = options.Position ?? _config!.Position;
            AnimationMode animation = options.Animation ?? _config!.AnimationMode;
            TextOverflowMode textOverflow = options.TextOverflow ?? _config!.TextOverflowMode;

            long id = _engine!.NextNotificationId();

            var model = new NotificationModel(
                id,
                options.Type,
                options.Title,
                options.Message,
                options.Icon,
                options.CustomTypeBadge,
                template,
                priority,
                options.GroupKey ?? string.Empty,
                DateTime.UtcNow,
                displayDuration,
                target,
                position,
                animation,
                textOverflow,
                options.Actions,
                options.OnClick,
                options.OnDoubleClick,
                options.OnClose,
                options.Tag);

            return _engine.Submit(model);
        }

        #endregion

        #region History & Notification Center Management

        /// <summary>
        /// Marks the notification with the specified ID as read.
        /// </summary>
        public static void MarkAsRead(long notificationId)
        {
            AssertInitialized();
            _historyService?.MarkAsRead(notificationId);
        }

        /// <summary>
        /// Marks all stored notifications as read.
        /// </summary>
        public static void MarkAllAsRead()
        {
            AssertInitialized();
            _historyService?.MarkAllAsRead();
        }

        /// <summary>
        /// Clears all stored notification history.
        /// </summary>
        public static void ClearHistory()
        {
            AssertInitialized();
            _historyService?.Clear();
        }

        /// <summary>
        /// Opens and displays the built-in Notification Center window.
        /// </summary>
        public static void OpenNotificationCenter()
        {
            AssertInitialized();
            _uiDispatcher?.Post(() =>
            {
                if (_notificationCenterForm != null && !_notificationCenterForm.IsDisposed)
                {
                    _notificationCenterForm.ReloadItems();
                    AnchorNotificationCenter();
                    _notificationCenterForm.Show();
                    _notificationCenterForm.BringToFront();
                }
            });
        }

        /// <summary>
        /// Closes/hides the built-in Notification Center window.
        /// </summary>
        public static void CloseNotificationCenter()
        {
            AssertInitialized();
            _uiDispatcher?.Post(() =>
            {
                if (_notificationCenterForm != null && !_notificationCenterForm.IsDisposed)
                {
                    _notificationCenterForm.Hide();
                }
            });
        }

        /// <summary>
        /// Toggles visibility of the built-in Notification Center window.
        /// </summary>
        public static void ToggleNotificationCenter()
        {
            AssertInitialized();
            _uiDispatcher?.Post(() =>
            {
                if (_notificationCenterForm != null && !_notificationCenterForm.IsDisposed)
                {
                    if (_notificationCenterForm.Visible)
                    {
                        _notificationCenterForm.Hide();
                    }
                    else
                    {
                        OpenNotificationCenter();
                    }
                }
            });
        }

        private static void AnchorNotificationCenter()
        {
            if (_notificationCenterForm == null || _notificationCenterForm.IsDisposed) return;

            // Anchor near floating icon if visible, otherwise anchor to bottom-right of host form
            if (_floatingIconForm != null && _floatingIconForm.Visible && !_floatingIconForm.IsDisposed)
            {
                int x = _floatingIconForm.Left - _notificationCenterForm.Width - 10;
                int y = _floatingIconForm.Bottom - _notificationCenterForm.Height;
                if (x < 10) x = _floatingIconForm.Right + 10;
                _notificationCenterForm.Location = new Point(Math.Max(10, x), Math.Max(10, y));
            }
            else if (_config?.HostForm != null && _config.HostForm.IsHandleCreated && !_config.HostForm.IsDisposed)
            {
                Point screenPt = _config.HostForm.PointToScreen(new Point(
                    _config.HostForm.ClientSize.Width - _notificationCenterForm.Width - 20,
                    _config.HostForm.ClientSize.Height - _notificationCenterForm.Height - 20));
                _notificationCenterForm.Location = screenPt;
            }
        }

        /// <summary>
        /// Shows or hides the floating draggable notification icon.
        /// </summary>
        public static void SetFloatingIconVisible(bool visible)
        {
            AssertInitialized();
            _uiDispatcher?.Post(() =>
            {
                if (_floatingIconForm != null && !_floatingIconForm.IsDisposed)
                {
                    if (visible) _floatingIconForm.Show();
                    else _floatingIconForm.Hide();
                }
            });
        }

        #endregion

        /// <summary>
        /// Gracefully shuts down the current Notifier instance and re-initializes with the new configuration.
        /// Useful for interactive configuration tools, runtime theme/layout switching, or test environments.
        /// </summary>
        /// <param name="config">New master configuration.</param>
        public static void Reinitialize(NotificationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            lock (_initLock)
            {
                Dispose();
                Initialize(config);
            }
        }

        #region Safe Deadlock-Free Disposal

        /// <summary>
        /// Completely shuts down the notification engine and releases all resources.
        /// Thread-safe and callable from any thread (UI or worker thread).
        /// Immediately rejects new submissions, discards pending notifications, stops timers,
        /// destroys UI forms on the owning UI thread without blocking, and releases GDI handles.
        /// Idempotent.
        /// </summary>
        public static void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            Interlocked.Exchange(ref _isInitialized, 0);

            // 1. Immediately stop core engine, reject new submissions, and discard pending queues
            try
            {
                _engine?.Dispose();
                _engine = null;
            }
            catch { }

            // 2. Tear down UI resources on UI thread non-blockingly (never deadlock worker thread)
            _uiDispatcher?.Post(() =>
            {
                try
                {
                    _floatingIconForm?.Close();
                    _floatingIconForm?.Dispose();
                    _floatingIconForm = null;
                }
                catch { }

                try
                {
                    _notificationCenterForm?.Close();
                    _notificationCenterForm?.Dispose();
                    _notificationCenterForm = null;
                }
                catch { }

                try
                {
                    _toastManager?.Dispose();
                    _toastManager = null;
                }
                catch { }

                try
                {
                    _windowsNotificationService?.Dispose();
                    _windowsNotificationService = null;
                }
                catch { }

                try
                {
                    GdiResourceCache.Instance.Dispose();
                }
                catch { }
            });

            // 3. Dispose history service
            try
            {
                _historyService?.Dispose();
                _historyService = null;
            }
            catch { }
        }

        private static void AssertInitialized()
        {
            if (Volatile.Read(ref _isDisposed) == 1)
            {
                throw new ObjectDisposedException(nameof(SanketDesk), "SanketDesk has been disposed.");
            }
            if (Volatile.Read(ref _isInitialized) == 0)
            {
                throw new InvalidOperationException("SanketDesk is not initialized. Call SanketDesk.Initialize(config) first.");
            }
        }

        #endregion
    }
}
