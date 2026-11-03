#nullable enable
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Notifier;

namespace Net8Sample
{
    public sealed class MainForm : Form
    {
        private readonly Label _statusLabel;

        public MainForm()
        {
            Text = "Production WinForms Notifier Showcase (.NET 8)";
            Size = new Size(820, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // Initialize Notifier
            var config = new NotificationConfig
            {
                HostForm = this,
                DefaultTemplate = NotificationTemplateNames.ModernCard,
                Position = NotificationPosition.BottomRight,
                BurstStrategy = BurstStrategy.CoalesceByKey,
                GroupingStrategy = NotificationGroupingStrategy.ByKeyOrTitle,
                EnableHistory = true,
                MaxHistoryCount = 500,
                EnableFloatingIcon = true,
                KeepFloatingIconOnAppDeactivate = false
            };
            Notifier.Notifier.Initialize(config);

            // Layout UI
            var titleLabel = new Label
            {
                Text = "Windows Forms Notification Component Suite",
                Font = new Font("Segoe UI", 15.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(30, 20),
                AutoSize = true
            };
            Controls.Add(titleLabel);

            var subLabel = new Label
            {
                Text = "Commercial-quality notification engine with 10k/sec burst handling, templates, floating icon & notification center.",
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(32, 55),
                AutoSize = true
            };
            Controls.Add(subLabel);

            // Group 1: Basic Notifications
            var grpBasic = CreateGroupBox("Basic Notifications", 30, 90, 360, 160);
            AddButton(grpBasic, "Show Success", 20, 30, 145, 36, Color.FromArgb(40, 167, 69), () =>
                Notifier.Notifier.ShowSuccess("Order Created", "Order #40921 has been placed successfully."));
            AddButton(grpBasic, "Show Info", 185, 30, 145, 36, Color.FromArgb(23, 162, 184), () =>
                Notifier.Notifier.ShowInfo("Sync Complete", "Database synchronizer finished processing 124 records."));
            AddButton(grpBasic, "Show Warning", 20, 80, 145, 36, Color.FromArgb(255, 179, 0), () =>
                Notifier.Notifier.ShowWarning("Network Latency", "High round-trip latency detected on primary gateway."));
            AddButton(grpBasic, "Show Error", 185, 80, 145, 36, Color.FromArgb(220, 53, 69), () =>
                Notifier.Notifier.ShowError("Payment Failed", "Card authorization declined by issuing bank (Code: 51)."));
            Controls.Add(grpBasic);

            // Group 2: Visual Template Families
            var grpTemplates = CreateGroupBox("Visual Template Families", 420, 90, 360, 230);
            AddButton(grpTemplates, "ModernCard", 20, 30, 145, 36, Color.FromArgb(75, 85, 99), () =>
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.ModernCard,
                    Type = NotificationType.Success,
                    Title = "Modern Card",
                    Message = "Clean card with subtle shadow, status dot, and progress bar."
                }));
            AddButton(grpTemplates, "MinimalBar", 185, 30, 145, 36, Color.FromArgb(75, 85, 99), () =>
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.MinimalBar,
                    Type = NotificationType.Info,
                    Title = "Minimal Bar",
                    Message = "Compact bar with colored left accent line and inline layout."
                }));
            AddButton(grpTemplates, "SolidAccent", 20, 75, 145, 36, Color.FromArgb(75, 85, 99), () =>
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.SolidAccent,
                    Type = NotificationType.Warning,
                    Title = "Solid Accent",
                    Message = "Vibrant full-color background with crisp white typography."
                }));
            AddButton(grpTemplates, "SplitPill", 185, 75, 145, 36, Color.FromArgb(75, 85, 99), () =>
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.SplitPill,
                    Type = NotificationType.Error,
                    Title = "Split Pill",
                    Message = "Two-tone card with solid left icon column and white body."
                }));
            AddButton(grpTemplates, "ActionPrompt (Undo)", 20, 120, 310, 36, Color.FromArgb(37, 99, 235), () =>
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.ActionPrompt,
                    Type = NotificationType.Warning,
                    Title = "File Deleted",
                    Message = "The file 'benchmark_results.csv' was moved to trash.",
                    Actions = new[]
                    {
                        new NotificationAction("Undo", id => Notifier.Notifier.ShowSuccess("Restored", "File 'benchmark_results.csv' restored.")),
                        new NotificationAction("Dismiss", id => { })
                    }
                }));
            AddButton(grpTemplates, "Windows Fluent", 20, 165, 310, 36, Color.FromArgb(30, 41, 59), () =>
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.WindowsFluent,
                    Type = NotificationType.Info,
                    CustomTypeBadge = "SYSTEM TASK",
                    Title = "Maintenance Window",
                    Message = "Scheduled server maintenance begins at 02:00 UTC.",
                    Actions = new[]
                    {
                        new NotificationAction("Acknowledge", id => { }),
                        new NotificationAction("Snooze", id => Notifier.Notifier.ShowInfo("Snoozed", "Reminder snoozed for 15 minutes."))
                    }
                }));
            Controls.Add(grpTemplates);

            // Group 3: High-Frequency Burst & Grouping
            var grpBurst = CreateGroupBox("Burst & Grouping Tests", 30, 260, 360, 200);
            AddButton(grpBurst, "100 Burst (Same Key -> Title (100))", 20, 30, 310, 36, Color.FromArgb(99, 102, 241), () =>
            {
                Task.Run(() =>
                {
                    for (int i = 1; i <= 100; i++)
                    {
                        Notifier.Notifier.Show(new AdvancedNotificationOptions
                        {
                            Type = NotificationType.Warning,
                            Title = "Database Lock Warning",
                            Message = "Row lock contention on table 'Orders'",
                            GroupKey = "DbLockContention"
                        });
                    }
                });
            });
            AddButton(grpBurst, "10,000 Burst (Stress Test)", 20, 75, 310, 36, Color.FromArgb(239, 68, 68), () =>
            {
                Task.Run(() =>
                {
                    Parallel.For(0, 10000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
                    {
                        Notifier.Notifier.Show(new AdvancedNotificationOptions
                        {
                            Type = NotificationType.Info,
                            Title = "Audit Stream Event",
                            Message = "High volume telemetry event #" + i,
                            GroupKey = "AuditStream"
                        });
                    });
                });
            });
            AddButton(grpBurst, "Debounce Delay (500ms)", 20, 120, 310, 36, () =>
            {
                Notifier.Notifier.Show(new AdvancedNotificationOptions
                {
                    Type = NotificationType.Info,
                    Title = "Search Query",
                    Message = "Debounced query dispatched after typing paused.",
                    GroupKey = "SearchDebounce",
                    DelayMode = NotificationDelayMode.Debounce,
                    Delay = TimeSpan.FromMilliseconds(500)
                });
            });
            Controls.Add(grpBurst);

            // Group 4: Notification Center & Floating Icon Controls
            var grpUi = CreateGroupBox("Notification Center & Floating Icon", 420, 330, 360, 130);
            AddButton(grpUi, "Open Notification Center", 20, 30, 310, 36, Color.FromArgb(79, 70, 229), () =>
                Notifier.Notifier.OpenNotificationCenter());
            AddButton(grpUi, "Mark All as Read", 20, 75, 145, 36, Color.FromArgb(107, 114, 128), () =>
                Notifier.Notifier.MarkAllAsRead());
            AddButton(grpUi, "Clear History", 185, 75, 145, 36, Color.FromArgb(220, 53, 69), () =>
                Notifier.Notifier.ClearHistory());
            Controls.Add(grpUi);

            // Status Bar
            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(229, 231, 235),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Text = $"Ready | Unread Count: {Notifier.Notifier.UnreadCount} | Status: Engine Active"
            };
            Controls.Add(_statusLabel);

            Notifier.Notifier.History.UnreadCountChanged += count =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke((Action)(() =>
                    {
                        _statusLabel.Text = $"Ready | Unread Count: {count} | Status: Engine Active";
                    }));
                }
            };
        }

        private static GroupBox CreateGroupBox(string title, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White
            };
        }

        private static void AddButton(GroupBox parent, string text, int x, int y, int w, int h, Action onClick)
        {
            AddButton(parent, text, x, y, w, h, Color.FromArgb(75, 85, 99), onClick);
        }

        private static void AddButton(GroupBox parent, string text, int x, int y, int w, int h, Color color, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            parent.Controls.Add(btn);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Notifier.Notifier.Dispose();
        }
    }
}
