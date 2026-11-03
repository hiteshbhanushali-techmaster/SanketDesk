#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Notifier
{
    /// <summary>
    /// Master configuration for the Notifier engine and UI component suite.
    /// Supplied during Notifier.Initialize(config). Once initialized, the configuration is frozen to guarantee thread-safety.
    /// </summary>
    public sealed class NotificationConfig
    {
        private bool _isFrozen;
        private Form? _hostForm;
        private string _defaultTemplate = NotificationTemplateNames.ModernCard;
        private TimeSpan _defaultDisplayDuration = TimeSpan.FromSeconds(5);
        private PriorityMode _priorityMode = PriorityMode.SystemPriority;
        private BurstStrategy _burstStrategy = BurstStrategy.CoalesceByKey;
        private NotificationGroupingStrategy _groupingStrategy = NotificationGroupingStrategy.ByKeyOrTitle;
        private int _maxPendingQueueCapacity = 10000;
        private NotificationDelayMode _delayMode = NotificationDelayMode.None;
        private TimeSpan _delayInterval = TimeSpan.Zero;
        private NotificationPosition _position = NotificationPosition.BottomRight;
        private int _horizontalOffset = 20;
        private int _verticalOffset = 20;
        private int _spacing = 10;
        private int _maxVisibleNotifications = 5;
        private TextOverflowMode _textOverflowMode = TextOverflowMode.Ellipsis;
        private AnimationMode _animationMode = AnimationMode.None;
        private NotificationTarget _target = NotificationTarget.InApp;

        // History & Persistence
        private bool _enableHistory = true;
        private int _maxHistoryCount = 500;
        private TimeSpan _maxRetentionPeriod = TimeSpan.FromHours(24);
        private StorageType _storageType = StorageType.Memory;
        private string? _storagePath;

        // Floating Icon
        private bool _enableFloatingIcon = false;
        private bool _keepFloatingIconOnAppDeactivate = false;
        private Image? _floatingIconImage;

        // Windows Native Notifications
        private string? _windowsAppUserModelId;

        /// <summary>
        /// Gets or sets the primary host WinForms application form.
        /// Used for screen positioning, owner relationships, and floating icon boundary constraints.
        /// </summary>
        public Form? HostForm
        {
            get => _hostForm;
            set { AssertNotFrozen(); _hostForm = value; }
        }

        /// <summary>
        /// Gets or sets the default template name used when a notification does not specify one.
        /// Default is <see cref="NotificationTemplateNames.ModernCard"/>.
        /// </summary>
        public string DefaultTemplate
        {
            get => _defaultTemplate;
            set { AssertNotFrozen(); _defaultTemplate = value ?? NotificationTemplateNames.ModernCard; }
        }

        /// <summary>
        /// Gets or sets the default on-screen display duration. Default is 5 seconds.
        /// </summary>
        public TimeSpan DefaultDisplayDuration
        {
            get => _defaultDisplayDuration;
            set
            {
                AssertNotFrozen();
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "DefaultDisplayDuration must be positive.");
                _defaultDisplayDuration = value;
            }
        }

        /// <summary>
        /// Gets or sets the priority processing mode (SystemPriority: Error > Warning > Info > Success, or EqualPriority: FIFO).
        /// </summary>
        public PriorityMode PriorityMode
        {
            get => _priorityMode;
            set { AssertNotFrozen(); _priorityMode = value; }
        }

        /// <summary>
        /// Gets or sets the burst strategy applied when incoming request frequency is high.
        /// </summary>
        public BurstStrategy BurstStrategy
        {
            get => _burstStrategy;
            set { AssertNotFrozen(); _burstStrategy = value; }
        }

        /// <summary>
        /// Gets or sets the grouping strategy used to aggregate notifications into "Title (N)".
        /// </summary>
        public NotificationGroupingStrategy GroupingStrategy
        {
            get => _groupingStrategy;
            set { AssertNotFrozen(); _groupingStrategy = value; }
        }

        /// <summary>
        /// Gets or sets the hard upper bound for the pending queue capacity. Default is 10,000.
        /// </summary>
        public int MaxPendingQueueCapacity
        {
            get => _maxPendingQueueCapacity;
            set
            {
                AssertNotFrozen();
                if (value < 10)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxPendingQueueCapacity must be at least 10.");
                _maxPendingQueueCapacity = value;
            }
        }

        /// <summary>
        /// Gets or sets the scheduling delay mode (None, Fixed, Debounce, Batch).
        /// </summary>
        public NotificationDelayMode DelayMode
        {
            get => _delayMode;
            set { AssertNotFrozen(); _delayMode = value; }
        }

        /// <summary>
        /// Gets or sets the delay or debounce interval.
        /// </summary>
        public TimeSpan DelayInterval
        {
            get => _delayInterval;
            set
            {
                AssertNotFrozen();
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "DelayInterval cannot be negative.");
                _delayInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the anchor position on the target monitor. Default is BottomRight.
        /// </summary>
        public NotificationPosition Position
        {
            get => _position;
            set { AssertNotFrozen(); _position = value; }
        }

        /// <summary>
        /// Gets or sets the horizontal offset in pixels from the screen/working area edge. Default is 20px.
        /// </summary>
        public int HorizontalOffset
        {
            get => _horizontalOffset;
            set { AssertNotFrozen(); _horizontalOffset = Math.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets the vertical offset in pixels from the screen/working area edge. Default is 20px.
        /// </summary>
        public int VerticalOffset
        {
            get => _verticalOffset;
            set { AssertNotFrozen(); _verticalOffset = Math.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets the vertical spacing in pixels between stacked toast cards. Default is 10px.
        /// </summary>
        public int Spacing
        {
            get => _spacing;
            set { AssertNotFrozen(); _spacing = Math.Max(0, value); }
        }

        /// <summary>
        /// Gets or sets the maximum number of active visible toast windows displayed simultaneously. Default is 5.
        /// </summary>
        public int MaxVisibleNotifications
        {
            get => _maxVisibleNotifications;
            set
            {
                AssertNotFrozen();
                if (value < 1 || value > 20)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxVisibleNotifications must be between 1 and 20.");
                _maxVisibleNotifications = value;
            }
        }

        /// <summary>
        /// Gets or sets the text overflow layout mode. Default is Ellipsis.
        /// </summary>
        public TextOverflowMode TextOverflowMode
        {
            get => _textOverflowMode;
            set { AssertNotFrozen(); _textOverflowMode = value; }
        }

        /// <summary>
        /// Gets or sets the transition animation mode. Default is None.
        /// </summary>
        public AnimationMode AnimationMode
        {
            get => _animationMode;
            set { AssertNotFrozen(); _animationMode = value; }
        }

        /// <summary>
        /// Gets or sets the notification delivery target (InApp, Windows native toast/balloon, or Both). Default is InApp.
        /// </summary>
        public NotificationTarget Target
        {
            get => _target;
            set { AssertNotFrozen(); _target = value; }
        }

        /// <summary>
        /// Gets or sets whether history tracking is enabled. Default is true.
        /// </summary>
        public bool EnableHistory
        {
            get => _enableHistory;
            set { AssertNotFrozen(); _enableHistory = value; }
        }

        /// <summary>
        /// Gets or sets the hard limit on the number of stored history records.
        /// When exceeded, oldest records are pruned first (strictly FIFO / oldest sequence).
        /// Default is 500.
        /// </summary>
        public int MaxHistoryCount
        {
            get => _maxHistoryCount;
            set
            {
                AssertNotFrozen();
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxHistoryCount must be at least 1.");
                _maxHistoryCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the hard limit on the history retention period. Default is 24 hours.
        /// </summary>
        public TimeSpan MaxRetentionPeriod
        {
            get => _maxRetentionPeriod;
            set
            {
                AssertNotFrozen();
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "MaxRetentionPeriod must be positive.");
                _maxRetentionPeriod = value;
            }
        }

        /// <summary>
        /// Gets or sets the persistence storage provider (Memory, Json, Sqlite). Default is Memory.
        /// </summary>
        public StorageType StorageType
        {
            get => _storageType;
            set { AssertNotFrozen(); _storageType = value; }
        }

        /// <summary>
        /// Gets or sets the file path for JSON or SQLite storage. If null, default application data path is used.
        /// </summary>
        public string? StoragePath
        {
            get => _storagePath;
            set { AssertNotFrozen(); _storagePath = value; }
        }

        /// <summary>
        /// Gets or sets whether the floating draggable notification icon is enabled. Default is false.
        /// </summary>
        public bool EnableFloatingIcon
        {
            get => _enableFloatingIcon;
            set { AssertNotFrozen(); _enableFloatingIcon = value; }
        }

        /// <summary>
        /// Gets or sets whether the floating icon remains visible when the host application loses focus. Default is false.
        /// </summary>
        public bool KeepFloatingIconOnAppDeactivate
        {
            get => _keepFloatingIconOnAppDeactivate;
            set { AssertNotFrozen(); _keepFloatingIconOnAppDeactivate = value; }
        }

        /// <summary>
        /// Gets or sets an optional custom icon image for the floating icon. If null, a built-in bell icon is rendered.
        /// </summary>
        public Image? FloatingIconImage
        {
            get => _floatingIconImage;
            set { AssertNotFrozen(); _floatingIconImage = value; }
        }

        /// <summary>
        /// Gets or sets the Windows App User Model ID (AUMID) used for Windows 10/11 native toast notifications.
        /// </summary>
        public string? WindowsAppUserModelId
        {
            get => _windowsAppUserModelId;
            set { AssertNotFrozen(); _windowsAppUserModelId = value; }
        }

        /// <summary>
        /// Freezes configuration so that properties cannot be modified at runtime.
        /// </summary>
        public void Freeze()
        {
            _isFrozen = true;
        }

        public bool IsFrozen => _isFrozen;

        private void AssertNotFrozen()
        {
            if (_isFrozen)
            {
                throw new InvalidOperationException("NotificationConfig cannot be modified after Notifier has been initialized.");
            }
        }

        /// <summary>
        /// Validates configuration values.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_defaultTemplate))
                _defaultTemplate = NotificationTemplateNames.ModernCard;

            if (_defaultDisplayDuration <= TimeSpan.Zero)
                _defaultDisplayDuration = TimeSpan.FromSeconds(5);

            if (_maxPendingQueueCapacity < 10)
                _maxPendingQueueCapacity = 10;

            if (_maxVisibleNotifications < 1)
                _maxVisibleNotifications = 1;

            if (_maxHistoryCount < 1)
                _maxHistoryCount = 500;

            if (_maxRetentionPeriod <= TimeSpan.Zero)
                _maxRetentionPeriod = TimeSpan.FromHours(24);
        }
    }
}
