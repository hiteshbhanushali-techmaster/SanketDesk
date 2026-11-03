#nullable enable
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConfiguratorSample.Forms;
using ConfiguratorSample.Models;
using SanketDesk;

namespace ConfiguratorSample
{
    /// <summary>
    /// Interactive Notifier Configurator and Playground.
    /// Lets the user test all engine and toast configurations via PropertyGrid,
    /// simulate all visual scenarios, and export C# integration code.
    /// </summary>
    public sealed class ConfiguratorForm : Form
    {
        private readonly NotificationConfigSettings _configSettings;
        private readonly ToastCustomOptions _toastOptions;

        private readonly PropertyGrid _propertyGrid;
        private readonly RadioButton _rbEngineConfig;
        private readonly RadioButton _rbToastConfig;
        private readonly CheckBox _chkAutoApply;
        private readonly Label _statusLabel;
        private readonly Label _eventLogLabel;

        private int _customToastCounter = 1;

        public ConfiguratorForm()
        {
            _configSettings = new NotificationConfigSettings();
            _toastOptions = new ToastCustomOptions();

            Text = "Notifier Interactive Configurator & Studio";
            Size = new Size(1220, 800);
            MinimumSize = new Size(1000, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // Controls
            _propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                SelectedObject = _configSettings,
                ToolbarVisible = true,
                HelpVisible = true,
                PropertySort = PropertySort.CategorizedAlphabetical,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                LineColor = Color.FromArgb(226, 232, 240),
                CategoryForeColor = Color.FromArgb(30, 41, 59),
                HelpBackColor = Color.FromArgb(241, 245, 249),
                HelpForeColor = Color.FromArgb(15, 23, 42)
            };

            _rbEngineConfig = new RadioButton
            {
                Text = "Engine Master Config",
                Location = new Point(12, 10),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            _rbToastConfig = new RadioButton
            {
                Text = "Toast Custom Options",
                Location = new Point(180, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            _chkAutoApply = new CheckBox
            {
                Text = "Auto-Apply on Property Change",
                Location = new Point(12, 12),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _statusLabel = new Label
            {
                Text = "Engine Active | Position: BottomRight | Unread: 0",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(16, 8),
                AutoSize = true
            };

            _eventLogLabel = new Label
            {
                Text = "Ready for user interactions.",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(480, 8),
                AutoSize = true
            };

            // Initialize Notifier initially with default config
            InitializeNotifierEngine();

            // 1. Top Header Bar
            var topPanel = BuildHeaderBar();
            Controls.Add(topPanel);

            // 2. Status Bar at Bottom
            var bottomStatusBar = BuildStatusBar();
            Controls.Add(bottomStatusBar);

            // 3. Central SplitContainer: Left = PropertyGrid Studio, Right = Action & Testing Studio
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 460,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(226, 232, 240)
            };

            // Left Panel: Configuration Studio
            var leftPanel = BuildLeftStudioPanel();
            split.Panel1.Controls.Add(leftPanel);

            // Right Panel: Action & Simulation Studio
            var rightPanel = BuildRightSimulationPanel();
            split.Panel2.Controls.Add(rightPanel);

            Controls.Add(split);

            // Ensure proper docking order
            split.BringToFront();

            // Wire up history unread count updates
            AttachHistoryEvents();
        }

        private void InitializeNotifierEngine()
        {
            try
            {
                var config = _configSettings.ToNotificationConfig(this);
                if (SanketDeskNotifier.IsInitialized)
                {
                    SanketDeskNotifier.Reinitialize(config);
                }
                else
                {
                    SanketDeskNotifier.Initialize(config);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to initialize Notifier: " + ex.Message, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel BuildHeaderBar()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(24, 12, 24, 12)
            };

            var titleLabel = new Label
            {
                Text = "Notifier Interactive Studio & Configurator",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 10),
                AutoSize = true
            };
            header.Controls.Add(titleLabel);

            string runtime =
#if NET8_0_OR_GREATER
                ".NET 8.0 Windows";
#else
                ".NET Framework 4.6.1+";
#endif

            var subLabel = new Label
            {
                Text = $"Active Runtime: {runtime} | Pure Windows Forms Component Suite",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(26, 38),
                AutoSize = true
            };
            header.Controls.Add(subLabel);

            // Button: View C# Integration Code
            var btnViewCode = new Button
            {
                Text = "Export C# Integration Code",
                Width = 210,
                Height = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(header.Width - 460, 16),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnViewCode.FlatAppearance.BorderSize = 0;
            btnViewCode.Click += (s, e) =>
            {
                using var codeForm = new CodeExportForm(_configSettings, _toastOptions);
                codeForm.ShowDialog(this);
            };
            header.Controls.Add(btnViewCode);

            // Button: Apply Configuration
            var btnApply = new Button
            {
                Text = "Apply Configuration",
                Width = 150,
                Height = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(header.Width - 240, 16),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) => ApplyConfiguration();
            header.Controls.Add(btnApply);

            // Button: Reset Defaults
            var btnReset = new Button
            {
                Text = "Reset",
                Width = 75,
                Height = 38,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(header.Width - 80, 16),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.Click += (s, e) => ResetDefaults();
            header.Controls.Add(btnReset);

            return header;
        }

        private Panel BuildLeftStudioPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12)
            };

            // Switcher Panel
            var switcherPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(8, 6, 8, 6)
            };

            switcherPanel.Controls.Add(_rbEngineConfig);
            switcherPanel.Controls.Add(_rbToastConfig);
            panel.Controls.Add(switcherPanel);

            // Bottom Actions Panel in Left Split
            var bottomActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(10, 8, 10, 8)
            };

            bottomActions.Controls.Add(_chkAutoApply);
            panel.Controls.Add(bottomActions);

            _propertyGrid.PropertyValueChanged += (s, e) =>
            {
                if (_chkAutoApply.Checked && _rbEngineConfig.Checked)
                {
                    ApplyConfiguration();
                }
            };

            _rbEngineConfig.CheckedChanged += (s, e) =>
            {
                if (_rbEngineConfig.Checked)
                {
                    _propertyGrid.SelectedObject = _configSettings;
                }
            };

            _rbToastConfig.CheckedChanged += (s, e) =>
            {
                if (_rbToastConfig.Checked)
                {
                    _propertyGrid.SelectedObject = _toastOptions;
                }
            };

            panel.Controls.Add(_propertyGrid);

            // Z-Order
            _propertyGrid.BringToFront();

            return panel;
        }

        private Panel BuildRightSimulationPanel()
        {
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16)
            };

            int y = 16;
            int cardWidth = 680;

            // 1. Group: Basic Notifications
            var grpBasic = CreateCardBox("1. Basic Notifications (Built-in Types)", 16, y, cardWidth, 100);
            AddButton(grpBasic, "Show Success", 16, 32, 150, 40, Color.FromArgb(16, 185, 129), () =>
            {
                SanketDeskNotifier.ShowSuccess("Operation Successful", "Changes were committed to the primary replica.");
                LogEvent("Triggered Success Toast");
            });
            AddButton(grpBasic, "Show Info", 176, 32, 150, 40, Color.FromArgb(14, 165, 233), () =>
            {
                SanketDeskNotifier.ShowInfo("Sync In Progress", "124 batch items processed successfully.");
                LogEvent("Triggered Info Toast");
            });
            AddButton(grpBasic, "Show Warning", 336, 32, 150, 40, Color.FromArgb(245, 158, 11), () =>
            {
                SanketDeskNotifier.ShowWarning("Disk Space Alert", "Volume C: has dropped below 15% free space.");
                LogEvent("Triggered Warning Toast");
            });
            AddButton(grpBasic, "Show Error", 496, 32, 150, 40, Color.FromArgb(239, 68, 68), () =>
            {
                SanketDeskNotifier.ShowError("Connection Aborted", "Unable to establish socket to host server.");
                LogEvent("Triggered Error Toast");
            });
            scrollPanel.Controls.Add(grpBasic);
            y += 114;

            // 2. Group: Visual Template Gallery
            var grpTemplates = CreateCardBox("2. Visual Template Gallery (6 Built-in Styles)", 16, y, cardWidth, 145);
            AddButton(grpTemplates, "ModernCard", 16, 30, 205, 36, Color.FromArgb(51, 65, 85), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.ModernCard,
                    Type = NotificationType.Success,
                    Title = "Modern Card Template",
                    Message = "Subtle shadow, progress line, and clean status indicator."
                });
                LogEvent("Triggered ModernCard Template");
            });
            AddButton(grpTemplates, "MinimalBar", 231, 30, 205, 36, Color.FromArgb(51, 65, 85), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.MinimalBar,
                    Type = NotificationType.Info,
                    Title = "Minimal Bar Template",
                    Message = "Compact card featuring colored left vertical accent line."
                });
                LogEvent("Triggered MinimalBar Template");
            });
            AddButton(grpTemplates, "SolidAccent", 446, 30, 205, 36, Color.FromArgb(51, 65, 85), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.SolidAccent,
                    Type = NotificationType.Warning,
                    Title = "Solid Accent Template",
                    Message = "Vibrant full-color background with crisp typography."
                });
                LogEvent("Triggered SolidAccent Template");
            });

            AddButton(grpTemplates, "SplitPill", 16, 78, 205, 36, Color.FromArgb(51, 65, 85), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.SplitPill,
                    Type = NotificationType.Error,
                    Title = "Split Pill Template",
                    Message = "Distinct two-tone design with solid icon column and white body."
                });
                LogEvent("Triggered SplitPill Template");
            });
            AddButton(grpTemplates, "ActionPrompt", 231, 78, 205, 36, Color.FromArgb(37, 99, 235), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.ActionPrompt,
                    Type = NotificationType.Warning,
                    Title = "File Deleted",
                    Message = "Moved 'document_v2.pdf' to trash.",
                    Actions = new[]
                    {
                        new NotificationAction("Undo", id => SanketDeskNotifier.ShowSuccess("Restored", "File restored.")),
                        new NotificationAction("Dismiss", id => { })
                    }
                });
                LogEvent("Triggered ActionPrompt Template");
            });
            AddButton(grpTemplates, "Windows Fluent", 446, 78, 205, 36, Color.FromArgb(15, 23, 42), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Template = NotificationTemplateNames.WindowsFluent,
                    Type = NotificationType.Info,
                    CustomTypeBadge = "FLUENT UI",
                    Title = "Acrylic Fluent Style",
                    Message = "Windows 11 inspired card appearance with subtle border."
                });
                LogEvent("Triggered WindowsFluent Template");
            });
            scrollPanel.Controls.Add(grpTemplates);
            y += 160;

            // 3. Group: Custom Toast Trigger
            var grpCustom = CreateCardBox("3. Custom Toast Simulator (Uses Active 'Toast Custom Options' Tab)", 16, y, cardWidth, 90);
            var btnFireCustom = new Button
            {
                Text = "Fire Custom Toast Now",
                Location = new Point(16, 28),
                Size = new Size(cardWidth - 32, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.0f, FontStyle.Bold),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnFireCustom.FlatAppearance.BorderSize = 0;
            btnFireCustom.Click += (s, e) =>
            {
                var options = _toastOptions.ToAdvancedNotificationOptions(msg => LogEvent(msg));
                SanketDeskNotifier.Show(options);
                LogEvent($"Fired Custom Toast #{_customToastCounter++} ('{options.Title}')");
            };
            grpCustom.Controls.Add(btnFireCustom);
            scrollPanel.Controls.Add(grpCustom);
            y += 105;

            // 4. Group: High-Frequency Bursts & Queueing
            var grpBurst = CreateCardBox("4. Burst Ingestion & Clubbing Scenarios", 16, y, cardWidth, 90);
            AddButton(grpBurst, "10-Burst (Club into Title (10))", 16, 30, 205, 38, Color.FromArgb(124, 58, 237), () =>
            {
                Task.Run(() =>
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        SanketDeskNotifier.Show(new AdvancedNotificationOptions
                        {
                            Type = NotificationType.Warning,
                            Title = "Database Lock Contention",
                            Message = "Row lock waiting on Orders table",
                            GroupKey = "DbLockWarning"
                        });
                    }
                });
                LogEvent("Dispatched 10-burst with shared GroupKey 'DbLockWarning'");
            });
            AddButton(grpBurst, "1,000-Burst Stress Test", 231, 30, 205, 38, Color.FromArgb(225, 29, 72), () =>
            {
                Task.Run(() =>
                {
                    Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
                    {
                        SanketDeskNotifier.Show(new AdvancedNotificationOptions
                        {
                            Type = NotificationType.Info,
                            Title = "Telemetry Stream",
                            Message = "High volume telemetry event #" + i,
                            GroupKey = "TelemetryStream"
                        });
                    });
                });
                LogEvent("Dispatched 1,000 parallel notifications");
            });
            AddButton(grpBurst, "Debounce Test (Click Fast)", 446, 30, 205, 38, Color.FromArgb(79, 70, 229), () =>
            {
                SanketDeskNotifier.Show(new AdvancedNotificationOptions
                {
                    Type = NotificationType.Info,
                    Title = "Search Query",
                    Message = "Debounced search query event fired.",
                    GroupKey = "DebouncedSearch",
                    DelayMode = NotificationDelayMode.Debounce,
                    Delay = TimeSpan.FromMilliseconds(500)
                });
                LogEvent("Triggered debounced toast (500ms debounce)");
            });
            scrollPanel.Controls.Add(grpBurst);
            y += 105;

            // 5. Group: Notification Center & Floating Icon Controls
            var grpCenter = CreateCardBox("5. Notification Center & Floating Icon", 16, y, cardWidth, 90);
            AddButton(grpCenter, "Toggle History Center", 16, 30, 150, 38, Color.FromArgb(79, 70, 229), () =>
            {
                SanketDeskNotifier.ToggleNotificationCenter();
                LogEvent("Toggled Notification Center window");
            });
            AddButton(grpCenter, "Mark All as Read", 176, 30, 150, 38, Color.FromArgb(100, 116, 139), () =>
            {
                SanketDeskNotifier.MarkAllAsRead();
                LogEvent("Marked all history notifications as read");
            });
            AddButton(grpCenter, "Clear History", 336, 30, 150, 38, Color.FromArgb(220, 38, 38), () =>
            {
                SanketDeskNotifier.ClearHistory();
                LogEvent("Cleared history storage");
            });
            AddButton(grpCenter, "Toggle Icon Visible", 496, 30, 150, 38, Color.FromArgb(71, 85, 105), () =>
            {
                bool isVisible = _configSettings.EnableFloatingIcon;
                _configSettings.EnableFloatingIcon = !isVisible;
                ApplyConfiguration();
                LogEvent($"Floating icon toggled to: {_configSettings.EnableFloatingIcon}");
            });
            scrollPanel.Controls.Add(grpCenter);

            return scrollPanel;
        }

        private Panel BuildStatusBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16, 0, 16, 0)
            };

            bar.Controls.Add(_statusLabel);
            bar.Controls.Add(_eventLogLabel);

            return bar;
        }

        private void ApplyConfiguration()
        {
            try
            {
                var config = _configSettings.ToNotificationConfig(this);
                SanketDeskNotifier.Reinitialize(config);

                _statusLabel.Text = $"Engine Active | Position: {_configSettings.Position} | Template: {_configSettings.DefaultTemplate} | Unread: {SanketDeskNotifier.UnreadCount}";
                LogEvent("Configuration successfully applied and engine re-initialized.");
            }
            catch (Exception ex)
            {
                LogEvent("Error applying config: " + ex.Message);
                MessageBox.Show(this, "Configuration validation failed: " + ex.Message, "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ResetDefaults()
        {
            var fresh = new NotificationConfigSettings();
            _configSettings.Position = fresh.Position;
            _configSettings.HorizontalOffset = fresh.HorizontalOffset;
            _configSettings.VerticalOffset = fresh.VerticalOffset;
            _configSettings.Spacing = fresh.Spacing;
            _configSettings.MaxVisibleNotifications = fresh.MaxVisibleNotifications;
            _configSettings.DefaultTemplate = fresh.DefaultTemplate;
            _configSettings.DefaultDisplayDurationSeconds = fresh.DefaultDisplayDurationSeconds;
            _configSettings.AnimationMode = fresh.AnimationMode;
            _configSettings.TextOverflowMode = fresh.TextOverflowMode;
            _configSettings.Target = fresh.Target;
            _configSettings.PriorityMode = fresh.PriorityMode;
            _configSettings.BurstStrategy = fresh.BurstStrategy;
            _configSettings.GroupingStrategy = fresh.GroupingStrategy;
            _configSettings.MaxPendingQueueCapacity = fresh.MaxPendingQueueCapacity;
            _configSettings.DelayMode = fresh.DelayMode;
            _configSettings.DelayIntervalMs = fresh.DelayIntervalMs;
            _configSettings.EnableHistory = fresh.EnableHistory;
            _configSettings.MaxHistoryCount = fresh.MaxHistoryCount;
            _configSettings.MaxRetentionHours = fresh.MaxRetentionHours;
            _configSettings.StorageType = fresh.StorageType;
            _configSettings.StoragePath = fresh.StoragePath;
            _configSettings.EnableFloatingIcon = fresh.EnableFloatingIcon;
            _configSettings.KeepFloatingIconOnAppDeactivate = fresh.KeepFloatingIconOnAppDeactivate;
            _configSettings.WindowsAppUserModelId = fresh.WindowsAppUserModelId;

            _propertyGrid.Refresh();
            ApplyConfiguration();
            LogEvent("Reset settings to factory defaults.");
        }

        private void AttachHistoryEvents()
        {
            if (SanketDeskNotifier.IsInitialized)
            {
                SanketDeskNotifier.History.UnreadCountChanged += count =>
                {
                    if (IsHandleCreated && !IsDisposed)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            _statusLabel.Text = $"Engine Active | Position: {_configSettings.Position} | Template: {_configSettings.DefaultTemplate} | Unread: {count}";
                        }));
                    }
                };
            }
        }

        private void LogEvent(string message)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke((Action)(() =>
                {
                    _eventLogLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
                }));
            }
        }

        private static GroupBox CreateCardBox(string title, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White
            };
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
            SanketDeskNotifier.Dispose();
        }
    }
}
