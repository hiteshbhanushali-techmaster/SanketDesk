# Getting Started with SanketDesk

This guide walks through installing and integrating SanketDesk into both .NET 8.0 and .NET Framework Windows Forms desktop applications.

---

## 1. Installation

Install the package matching your project's target framework:

### For Modern .NET (.NET 8.0 Windows or newer)
```bash
dotnet add package SanketDesk.Net8 --version 1.0.0
```

### For Legacy .NET Framework (.NET Framework 4.6.1 or newer)
```bash
dotnet add package SanketDesk.NetFramework --version 1.0.0
```

---

## 2. Basic Initialization

Initialize SanketDesk once during host application startup (e.g. in your `MainForm` constructor or `Program.Main`):

```csharp
using SanketDesk;
using System;
using System.Windows.Forms;

public class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        // Initialize with default configuration bound to this form
        Notifier.Initialize(new NotificationConfig
        {
            HostForm = this,
            Position = NotificationPosition.BottomRight,
            DefaultDisplayDuration = TimeSpan.FromSeconds(5)
        });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Clean shutdown
        Notifier.Dispose();
        base.OnFormClosing(e);
    }
}
```

---

## 3. Showing Notifications

### Simple Alerts
```csharp
Notifier.ShowSuccess("Connected", "Real-time feed connected.");
Notifier.ShowInfo("Sync Completed", "Batch sync completed.");
Notifier.ShowWarning("Disk Low", "C: has less than 10% free space.");
Notifier.ShowError("Order Failed", "Order rejected by exchange.");
```

### Advanced Toast with Custom Actions
```csharp
Notifier.Show(new AdvancedNotificationOptions
{
    Type = NotificationType.Warning,
    Title = "Network Reconnecting",
    Message = "Primary endpoint lost. Attempting auto-recovery...",
    Duration = TimeSpan.FromSeconds(8),
    Template = NotificationTemplateNames.ActionPrompt,
    Actions = new[]
    {
        new NotificationAction("Retry Now", id => ForceReconnect()),
        new NotificationAction("Dismiss", id => { })
    }
});
```

---

## 4. Next Steps
- See the [Configuration Reference](configuration.md) for custom visual themes and burst policies.
- Check the [Interactive Studio Guide](configuration.md#interactive-studio) to use the PropertyGrid configurator.
