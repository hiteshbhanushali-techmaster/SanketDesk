#nullable enable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Notifier;

namespace ConfiguratorSample.Models
{
    /// <summary>
    /// Editable wrapper for all Notifier configuration settings.
    /// Bound to PropertyGrid with full metadata, categories, and C# code generation.
    /// </summary>
    public sealed class NotificationConfigSettings
    {
        // Layout & Placement
        [Category("1. Layout & Placement")]
        [DisplayName("Anchor Position")]
        [Description("Screen anchor corner or center position for toast notifications on the host monitor.")]
        [DefaultValue(NotificationPosition.BottomRight)]
        public NotificationPosition Position { get; set; } = NotificationPosition.BottomRight;

        [Category("1. Layout & Placement")]
        [DisplayName("Horizontal Offset (px)")]
        [Description("Horizontal margin in pixels from the working screen/monitor edge.")]
        [DefaultValue(20)]
        public int HorizontalOffset { get; set; } = 20;

        [Category("1. Layout & Placement")]
        [DisplayName("Vertical Offset (px)")]
        [Description("Vertical margin in pixels from the working screen/monitor edge (does not obscure taskbar).")]
        [DefaultValue(20)]
        public int VerticalOffset { get; set; } = 20;

        [Category("1. Layout & Placement")]
        [DisplayName("Toast Spacing (px)")]
        [Description("Vertical gap in pixels between stacked active toast cards.")]
        [DefaultValue(10)]
        public int Spacing { get; set; } = 10;

        [Category("1. Layout & Placement")]
        [DisplayName("Max Visible Toasts")]
        [Description("Maximum number of active toast windows displayed simultaneously (1 to 20).")]
        [DefaultValue(5)]
        public int MaxVisibleNotifications { get; set; } = 5;

        // Visuals & Styling
        [Category("2. Visuals & Templates")]
        [DisplayName("Default Template")]
        [Description("Default visual card style used when a notification does not specify one.")]
        [TypeConverter(typeof(TemplateNameConverter))]
        [DefaultValue(NotificationTemplateNames.ModernCard)]
        public string DefaultTemplate { get; set; } = NotificationTemplateNames.ModernCard;

        [Category("2. Visuals & Templates")]
        [DisplayName("Display Duration (sec)")]
        [Description("Time in seconds the notification remains on screen before automatically fading/dismissing. Pauses on hover.")]
        [DefaultValue(5.0)]
        public double DefaultDisplayDurationSeconds { get; set; } = 5.0;

        [Category("2. Visuals & Templates")]
        [DisplayName("Animation Mode")]
        [Description("Transition animation applied when toast cards appear and dismiss.")]
        [DefaultValue(AnimationMode.None)]
        public AnimationMode AnimationMode { get; set; } = AnimationMode.None;

        [Category("2. Visuals & Templates")]
        [DisplayName("Text Overflow Mode")]
        [Description("Specifies how long message lines are formatted: Ellipsis (...) or AutoHeight.")]
        [DefaultValue(TextOverflowMode.Ellipsis)]
        public TextOverflowMode TextOverflowMode { get; set; } = TextOverflowMode.Ellipsis;

        [Category("2. Visuals & Templates")]
        [DisplayName("Delivery Target")]
        [Description("Target notification surface: InApp (WinForms toast), Windows (Action Center/Tray), or Both.")]
        [DefaultValue(NotificationTarget.InApp)]
        public NotificationTarget Target { get; set; } = NotificationTarget.InApp;

        // Queueing & High-Frequency Bursts
        [Category("3. Ingestion & Burst Handling")]
        [DisplayName("Priority Mode")]
        [Description("Priority processing rule: SystemPriority (Error > Warning > Info > Success) or EqualPriority (pure FIFO).")]
        [DefaultValue(PriorityMode.SystemPriority)]
        public PriorityMode PriorityMode { get; set; } = PriorityMode.SystemPriority;

        [Category("3. Ingestion & Burst Handling")]
        [DisplayName("Burst Strategy")]
        [Description("Engine admission policy under high frequency: Queue, Drop, Coalesce, or CoalesceByKey (Title (N)).")]
        [DefaultValue(BurstStrategy.CoalesceByKey)]
        public BurstStrategy BurstStrategy { get; set; } = BurstStrategy.CoalesceByKey;

        [Category("3. Ingestion & Burst Handling")]
        [DisplayName("Grouping Strategy")]
        [Description("Deterministic grouping criteria for burst clubbing: ByGroupKey, ByTitle, or ByKeyOrTitle.")]
        [DefaultValue(NotificationGroupingStrategy.ByKeyOrTitle)]
        public NotificationGroupingStrategy GroupingStrategy { get; set; } = NotificationGroupingStrategy.ByKeyOrTitle;

        [Category("3. Ingestion & Burst Handling")]
        [DisplayName("Max Pending Queue Capacity")]
        [Description("Hard upper capacity limit for the lock-free admission queue (minimum 10).")]
        [DefaultValue(10000)]
        public int MaxPendingQueueCapacity { get; set; } = 10000;

        [Category("3. Ingestion & Burst Handling")]
        [DisplayName("Delay Mode")]
        [Description("Scheduling delay policy: None (immediate), Fixed, Debounce, or Batch window.")]
        [DefaultValue(NotificationDelayMode.None)]
        public NotificationDelayMode DelayMode { get; set; } = NotificationDelayMode.None;

        [Category("3. Ingestion & Burst Handling")]
        [DisplayName("Delay Interval (ms)")]
        [Description("Delay duration in milliseconds used when DelayMode is Fixed, Debounce, or Batch.")]
        [DefaultValue(0)]
        public int DelayIntervalMs { get; set; } = 0;

        // History & Persistence
        [Category("4. History & Persistence")]
        [DisplayName("Enable History Tracking")]
        [Description("If true, notifications are indexed and retained in the built-in Notification Center.")]
        [DefaultValue(true)]
        public bool EnableHistory { get; set; } = true;

        [Category("4. History & Persistence")]
        [DisplayName("Max History Records")]
        [Description("Hard cap on retained history records. When exceeded, oldest items are pruned (strict FIFO).")]
        [DefaultValue(500)]
        public int MaxHistoryCount { get; set; } = 500;

        [Category("4. History & Persistence")]
        [DisplayName("Retention Period (hours)")]
        [Description("Hard cap in hours on history lifespan before records expire.")]
        [DefaultValue(24.0)]
        public double MaxRetentionHours { get; set; } = 24.0;

        [Category("4. History & Persistence")]
        [DisplayName("Storage Type")]
        [Description("Persistence backend: Memory, Json file, or Sqlite database.")]
        [DefaultValue(StorageType.Memory)]
        public StorageType StorageType { get; set; } = StorageType.Memory;

        [Category("4. History & Persistence")]
        [DisplayName("Storage Path")]
        [Description("Optional file path for JSON or SQLite history persistence. If blank, default app data is used.")]
        [DefaultValue(null)]
        public string? StoragePath { get; set; }

        // Floating Action Icon
        [Category("5. Floating Action Icon")]
        [DisplayName("Enable Floating Icon")]
        [Description("Displays a draggable, circular WhatsApp-style badge icon with live unread counter constrained to the host form.")]
        [DefaultValue(true)]
        public bool EnableFloatingIcon { get; set; } = true;

        [Category("5. Floating Action Icon")]
        [DisplayName("Keep Icon On App Deactivate")]
        [Description("Whether the floating icon stays visible when the host application loses focus.")]
        [DefaultValue(false)]
        public bool KeepFloatingIconOnAppDeactivate { get; set; } = false;

        // Windows Native
        [Category("6. Windows Native Notifications")]
        [DisplayName("Windows AUMID")]
        [Description("Windows App User Model ID (AUMID) for registering native Windows 10/11 Action Center toast notifications.")]
        [DefaultValue(null)]
        public string? WindowsAppUserModelId { get; set; }

        /// <summary>
        /// Converts the current settings into a runtime <see cref="NotificationConfig"/> instance.
        /// </summary>
        public NotificationConfig ToNotificationConfig(Form? hostForm)
        {
            var config = new NotificationConfig
            {
                HostForm = hostForm,
                Position = Position,
                HorizontalOffset = HorizontalOffset,
                VerticalOffset = VerticalOffset,
                Spacing = Spacing,
                MaxVisibleNotifications = Math.Max(1, Math.Min(20, MaxVisibleNotifications)),
                DefaultTemplate = !string.IsNullOrWhiteSpace(DefaultTemplate) ? DefaultTemplate : NotificationTemplateNames.ModernCard,
                DefaultDisplayDuration = TimeSpan.FromSeconds(Math.Max(0.5, DefaultDisplayDurationSeconds)),
                AnimationMode = AnimationMode,
                TextOverflowMode = TextOverflowMode,
                Target = Target,
                PriorityMode = PriorityMode,
                BurstStrategy = BurstStrategy,
                GroupingStrategy = GroupingStrategy,
                MaxPendingQueueCapacity = Math.Max(10, MaxPendingQueueCapacity),
                DelayMode = DelayMode,
                DelayInterval = TimeSpan.FromMilliseconds(Math.Max(0, DelayIntervalMs)),
                EnableHistory = EnableHistory,
                MaxHistoryCount = Math.Max(1, MaxHistoryCount),
                MaxRetentionPeriod = TimeSpan.FromHours(Math.Max(0.1, MaxRetentionHours)),
                StorageType = StorageType,
                StoragePath = !string.IsNullOrWhiteSpace(StoragePath) ? StoragePath : null,
                EnableFloatingIcon = EnableFloatingIcon,
                KeepFloatingIconOnAppDeactivate = KeepFloatingIconOnAppDeactivate,
                WindowsAppUserModelId = !string.IsNullOrWhiteSpace(WindowsAppUserModelId) ? WindowsAppUserModelId : null
            };

            return config;
        }

        /// <summary>
        /// Generates clean, ready-to-copy C# initialization code matching the current configuration.
        /// </summary>
        public string GenerateCSharpInitializationCode(string targetFramework = "net8")
        {
            var sb = new StringBuilder();
            bool isNet8 = targetFramework.Equals("net8", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine($"// Notifier Configuration (Targeting {(isNet8 ? ".NET 8.0 Windows" : ".NET Framework 4.6.1+")})");
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("using Notifier;");
            sb.AppendLine();
            sb.AppendLine("// In your main Form's constructor or Form_Load event:");
            sb.AppendLine("var config = new NotificationConfig");
            sb.AppendLine("{");
            sb.AppendLine("    HostForm = this,");
            sb.AppendLine($"    DefaultTemplate = NotificationTemplateNames.{DefaultTemplate},");
            sb.AppendLine($"    Position = NotificationPosition.{Position},");
            sb.AppendLine($"    HorizontalOffset = {HorizontalOffset},");
            sb.AppendLine($"    VerticalOffset = {VerticalOffset},");
            sb.AppendLine($"    Spacing = {Spacing},");
            sb.AppendLine($"    MaxVisibleNotifications = {MaxVisibleNotifications},");
            sb.AppendLine($"    DefaultDisplayDuration = TimeSpan.FromSeconds({DefaultDisplayDurationSeconds:0.##}),");
            sb.AppendLine($"    AnimationMode = AnimationMode.{AnimationMode},");
            sb.AppendLine($"    TextOverflowMode = TextOverflowMode.{TextOverflowMode},");
            sb.AppendLine($"    Target = NotificationTarget.{Target},");
            sb.AppendLine($"    PriorityMode = PriorityMode.{PriorityMode},");
            sb.AppendLine($"    BurstStrategy = BurstStrategy.{BurstStrategy},");
            sb.AppendLine($"    GroupingStrategy = NotificationGroupingStrategy.{GroupingStrategy},");
            sb.AppendLine($"    MaxPendingQueueCapacity = {MaxPendingQueueCapacity},");

            if (DelayMode != NotificationDelayMode.None)
            {
                sb.AppendLine($"    DelayMode = NotificationDelayMode.{DelayMode},");
                sb.AppendLine($"    DelayInterval = TimeSpan.FromMilliseconds({DelayIntervalMs}),");
            }

            sb.AppendLine($"    EnableHistory = {(EnableHistory ? "true" : "false")},");
            if (EnableHistory)
            {
                sb.AppendLine($"    MaxHistoryCount = {MaxHistoryCount},");
                sb.AppendLine($"    MaxRetentionPeriod = TimeSpan.FromHours({MaxRetentionHours:0.##}),");
                sb.AppendLine($"    StorageType = StorageType.{StorageType},");
                if (!string.IsNullOrWhiteSpace(StoragePath))
                {
                    sb.AppendLine($"    StoragePath = @\"{StoragePath}\",");
                }
            }

            sb.AppendLine($"    EnableFloatingIcon = {(EnableFloatingIcon ? "true" : "false")},");
            if (EnableFloatingIcon)
            {
                sb.AppendLine($"    KeepFloatingIconOnAppDeactivate = {(KeepFloatingIconOnAppDeactivate ? "true" : "false")},");
            }

            if (!string.IsNullOrWhiteSpace(WindowsAppUserModelId))
            {
                sb.AppendLine($"    WindowsAppUserModelId = \"{WindowsAppUserModelId}\",");
            }

            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("// Initialize the master notification engine (once per application)");
            sb.AppendLine("Notifier.Initialize(config);");

            return sb.ToString();
        }
    }
}
