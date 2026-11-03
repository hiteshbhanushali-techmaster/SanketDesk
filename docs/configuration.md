# SanketDesk Configuration Reference

This guide details all engine options, toast options, visual templates, and PropertyGrid studio controls in SanketDesk.

---

## 1. Engine Master Configuration (`NotificationConfig`)

Configured once during `Notifier.Initialize(config)`:

| Property | Type | Default | Description |
|---|---|---|---|
| `HostForm` | `Form` | `null` | The primary application form. Required for anchoring within window bounds and docking the floating badge. |
| `Position` | `NotificationPosition` | `BottomRight` | Screen dock location (`TopLeft`, `TopRight`, `TopCenter`, `BottomLeft`, `BottomRight`, `BottomCenter`, `Center`). |
| `MaxVisibleNotifications` | `int` | `3` | Maximum number of toast cards simultaneously visible on screen. Additional alerts queue in priority order. |
| `DefaultDisplayDuration` | `TimeSpan` | `5 seconds` | Time a toast remains on screen before automatically fading out. |
| `BurstStrategy` | `BurstStrategy` | `CoalesceByKey` | Strategy during high-frequency storms (`None`, `CoalesceByKey`, `CoalesceByTitle`, `Throttle`). |
| `MaxPendingQueueCapacity` | `int` | `1000` | Hard cap on pending queue to prevent unbounded memory growth. |
| `EnableHistory` | `bool` | `true` | When true, alerts are recorded in the notification history service. |
| `MaxHistoryCount` | `int` | `500` | Maximum items retained in memory/disk history (FIFO eviction). |
| `MaxRetentionPeriod` | `TimeSpan` | `24 hours` | Time-to-live limit for history entries. |
| `EnableFloatingIcon` | `bool` | `false` | When true, renders a draggable unread counter badge docked inside `HostForm`. |

---

## 2. Toast Options (`AdvancedNotificationOptions`)

Configured per individual notification submission via `Notifier.Show(...)`:

| Property | Type | Default | Description |
|---|---|---|---|
| `Type` | `NotificationType` | `Info` | `Success`, `Info`, `Warning`, `Error`. |
| `Title` | `string` | `""` | Notification title text. |
| `Message` | `string` | `""` | Notification message body text. |
| `Duration` | `TimeSpan?` | `null` | Custom display duration override for this notification. |
| `Template` | `string` | `ModernCard` | Name of the GDI+ renderer template to use. |
| `GroupKey` | `string` | `null` | Unique key for in-place coalescing and duplicate clubbing. |
| `CustomTypeBadge` | `string` | `null` | Custom text badge rendered in the header (e.g. `FATAL`, `CRITICAL`, `AUDIT`). |
| `Actions` | `NotificationAction[]`| `null` | Interactive buttons rendered at the bottom of the toast. |
| `Tag` | `object` | `null` | Arbitrary user payload or diagnostic token (`AlertActionToken`). |

---

## 3. Built-in GDI+ Visual Templates

SanketDesk includes 6 built-in vector visual templates:
1. `ModernCard` (Default): Sleek dark/light card with rounded corners, vector status icon, and clean typography.
2. `MinimalBar`: Compact horizontal strip for dense financial or status data.
3. `SolidAccent`: High-visibility toast with a solid color accent bar.
4. `SplitPill`: Rounded badge-style modern alert.
5. `ActionPrompt`: Interactive notification with prominent action buttons.
6. `WindowsFluent`: Windows 11 style translucent aesthetic with subtle border outlines.

---

## 4. Interactive PropertyGrid Configurator Studio

To test configurations interactively and generate C# code:
1. Launch `samples/ConfiguratorSample`.
2. Inspect the **Engine Master Config** or **Toast & Visual Config** tab in the PropertyGrid.
3. Tweak parameters (e.g., change `BurstStrategy` to `CoalesceByKey` or select `Template = ActionPrompt`).
4. Click **Trigger Toast** to see real-time animations.
5. Click **Export C# Code** to copy the generated initialization code.
