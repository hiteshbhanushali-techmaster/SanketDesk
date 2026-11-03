#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ConfiguratorSample.Models;
using Notifier;

namespace ConfiguratorSample.Forms
{
    /// <summary>
    /// Professional code export dialog displaying ready-to-use C# integration code
    /// tailored to the user's current PropertyGrid settings and selected runtime framework.
    /// </summary>
    public sealed class CodeExportForm : Form
    {
        private readonly NotificationConfigSettings _configSettings;
        private readonly ToastCustomOptions _toastOptions;

        private readonly RadioButton _rbNet8;
        private readonly RadioButton _rbNetFramework;
        private readonly ComboBox _cbSnippetType;
        private readonly TextBox _tbCode;
        private readonly Label _lblCopyNotice;

        public CodeExportForm(NotificationConfigSettings configSettings, ToastCustomOptions toastOptions)
        {
            _configSettings = configSettings ?? throw new ArgumentNullException(nameof(configSettings));
            _toastOptions = toastOptions ?? throw new ArgumentNullException(nameof(toastOptions));

            Text = "C# Project Integration Code Generator";
            Size = new Size(880, 680);
            MinimumSize = new Size(720, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);
            Icon = SystemIcons.Application;

            // Header Banner
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(24, 12, 24, 12)
            };

            var headerTitle = new Label
            {
                Text = "C# Integration Code Exporter",
                Font = new Font("Segoe UI", 13.0f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(24, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(headerTitle);

            var headerSub = new Label
            {
                Text = "Copy and paste this production-ready code into your Windows Forms desktop application.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(25, 38),
                AutoSize = true
            };
            headerPanel.Controls.Add(headerSub);
            Controls.Add(headerPanel);

            // Controls Bar (Framework Selector & Snippet Type)
            var controlBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(24, 10, 24, 10)
            };

            var lblFramework = new Label
            {
                Text = "Target Framework:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(24, 16),
                AutoSize = true
            };
            controlBar.Controls.Add(lblFramework);

            _rbNet8 = new RadioButton
            {
                Text = ".NET 8.0 Windows",
                Location = new Point(150, 14),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            _rbNet8.CheckedChanged += (s, e) => RefreshCode();
            controlBar.Controls.Add(_rbNet8);

            _rbNetFramework = new RadioButton
            {
                Text = ".NET Framework 4.6.1+",
                Location = new Point(290, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            _rbNetFramework.CheckedChanged += (s, e) => RefreshCode();
            controlBar.Controls.Add(_rbNetFramework);

            var lblSnippet = new Label
            {
                Text = "Snippet:",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(480, 16),
                AutoSize = true
            };
            controlBar.Controls.Add(lblSnippet);

            _cbSnippetType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(540, 13),
                Width = 260,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular)
            };
            _cbSnippetType.Items.AddRange(new object[]
            {
                "Complete Integration (Form Lifecycle)",
                "Initialization Code (NotificationConfig)",
                "Trigger Notification (Active Options)",
                "Basic Notifications (Success, Info, Error)",
                "Safe Teardown & Disposal (FormClosing)"
            });
            _cbSnippetType.SelectedIndex = 0;
            _cbSnippetType.SelectedIndexChanged += (s, e) => RefreshCode();
            controlBar.Controls.Add(_cbSnippetType);

            Controls.Add(controlBar);

            // Bottom Action Bar
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(24, 12, 24, 12)
            };

            var btnCopy = new Button
            {
                Text = "Copy Code to Clipboard",
                Width = 190,
                Height = 36,
                Location = new Point(24, 12),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += OnCopyClick;
            bottomBar.Controls.Add(btnCopy);

            var btnSave = new Button
            {
                Text = "Save to File...",
                Width = 130,
                Height = 36,
                Location = new Point(224, 12),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(15, 23, 42),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += OnSaveToFileClick;
            bottomBar.Controls.Add(btnSave);

            _lblCopyNotice = new Label
            {
                Text = "Code copied to clipboard!",
                ForeColor = Color.FromArgb(22, 101, 52),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Location = new Point(370, 20),
                AutoSize = true,
                Visible = false
            };
            bottomBar.Controls.Add(_lblCopyNotice);

            var btnClose = new Button
            {
                Text = "Close",
                Width = 95,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(bottomBar.Width - 120, 12),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BackColor = Color.FromArgb(203, 213, 225),
                ForeColor = Color.FromArgb(15, 23, 42),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();
            bottomBar.Controls.Add(btnClose);

            Controls.Add(bottomBar);

            // Code Container (Dark Monospace Terminal look)
            var codeContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            _tbCode = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10.0f, FontStyle.Regular),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.None
            };
            codeContainer.Controls.Add(_tbCode);
            Controls.Add(codeContainer);

            // Ensure correct dock z-order
            codeContainer.BringToFront();

            RefreshCode();
        }

        private void RefreshCode()
        {
            _lblCopyNotice.Visible = false;
            string targetFramework = _rbNet8.Checked ? "net8" : "net461";
            int snippetIndex = _cbSnippetType.SelectedIndex;

            switch (snippetIndex)
            {
                case 0:
                    _tbCode.Text = GenerateCompleteGuide(targetFramework);
                    break;
                case 1:
                    _tbCode.Text = _configSettings.GenerateCSharpInitializationCode(targetFramework);
                    break;
                case 2:
                    _tbCode.Text = _toastOptions.GenerateCSharpSubmissionCode();
                    break;
                case 3:
                    _tbCode.Text = GenerateBasicSnippets();
                    break;
                case 4:
                    _tbCode.Text = GenerateTeardownSnippet();
                    break;
                default:
                    _tbCode.Text = GenerateCompleteGuide(targetFramework);
                    break;
            }

            _tbCode.Select(0, 0);
        }

        private string GenerateCompleteGuide(string targetFramework)
        {
            bool isNet8 = targetFramework == "net8";
            var sb = new StringBuilder();

            sb.AppendLine("// ============================================================================");
            sb.AppendLine($"// Notifier Integration Walkthrough for {(isNet8 ? ".NET 8.0 Windows (net8.0-windows)" : ".NET Framework 4.6.1+ (net461)")}");
            sb.AppendLine("// Exclusively pure Windows Forms | Zero third-party runtime dependencies");
            sb.AppendLine("// ============================================================================");
            sb.AppendLine();
            sb.AppendLine("// STEP 1: Add Reference to your Project");
            if (isNet8)
            {
                sb.AppendLine("// In your .csproj file, add reference to Notifier.Net8.dll or project:");
                sb.AppendLine("// <ItemGroup>");
                sb.AppendLine("//   <Reference Include=\"Notifier.Net8\">");
                sb.AppendLine("//     <HintPath>path\\to\\Notifier.Net8.dll</HintPath>");
                sb.AppendLine("//   </Reference>");
                sb.AppendLine("// </ItemGroup>");
            }
            else
            {
                sb.AppendLine("// In your .csproj or References in Visual Studio, add reference to Notifier.NetFramework.dll:");
                sb.AppendLine("// <ItemGroup>");
                sb.AppendLine("//   <Reference Include=\"Notifier.NetFramework\">");
                sb.AppendLine("//     <HintPath>path\\to\\Notifier.NetFramework.dll</HintPath>");
                sb.AppendLine("//   </Reference>");
                sb.AppendLine("// </ItemGroup>");
            }
            sb.AppendLine();
            sb.AppendLine("// STEP 2: Configure and Initialize Notifier in your Main Form");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Windows.Forms;");
            sb.AppendLine("using Notifier;");
            sb.AppendLine();
            sb.AppendLine("public class MainForm : Form");
            sb.AppendLine("{");
            sb.AppendLine("    public MainForm()");
            sb.AppendLine("    {");
            sb.AppendLine("        InitializeComponent();");
            sb.AppendLine();
            sb.AppendLine("        // 1. Build configuration customized in Configurator Studio");
            sb.AppendLine("        var config = new NotificationConfig");
            sb.AppendLine("        {");
            sb.AppendLine("            HostForm = this,");
            sb.AppendLine($"            DefaultTemplate = NotificationTemplateNames.{_configSettings.DefaultTemplate},");
            sb.AppendLine($"            Position = NotificationPosition.{_configSettings.Position},");
            sb.AppendLine($"            HorizontalOffset = {_configSettings.HorizontalOffset},");
            sb.AppendLine($"            VerticalOffset = {_configSettings.VerticalOffset},");
            sb.AppendLine($"            Spacing = {_configSettings.Spacing},");
            sb.AppendLine($"            MaxVisibleNotifications = {_configSettings.MaxVisibleNotifications},");
            sb.AppendLine($"            DefaultDisplayDuration = TimeSpan.FromSeconds({_configSettings.DefaultDisplayDurationSeconds:0.##}),");
            sb.AppendLine($"            AnimationMode = AnimationMode.{_configSettings.AnimationMode},");
            sb.AppendLine($"            TextOverflowMode = TextOverflowMode.{_configSettings.TextOverflowMode},");
            sb.AppendLine($"            Target = NotificationTarget.{_configSettings.Target},");
            sb.AppendLine($"            PriorityMode = PriorityMode.{_configSettings.PriorityMode},");
            sb.AppendLine($"            BurstStrategy = BurstStrategy.{_configSettings.BurstStrategy},");
            sb.AppendLine($"            GroupingStrategy = NotificationGroupingStrategy.{_configSettings.GroupingStrategy},");
            sb.AppendLine($"            MaxPendingQueueCapacity = {_configSettings.MaxPendingQueueCapacity},");

            if (_configSettings.DelayMode != NotificationDelayMode.None)
            {
                sb.AppendLine($"            DelayMode = NotificationDelayMode.{_configSettings.DelayMode},");
                sb.AppendLine($"            DelayInterval = TimeSpan.FromMilliseconds({_configSettings.DelayIntervalMs}),");
            }

            sb.AppendLine($"            EnableHistory = {(_configSettings.EnableHistory ? "true" : "false")},");
            if (_configSettings.EnableHistory)
            {
                sb.AppendLine($"            MaxHistoryCount = {_configSettings.MaxHistoryCount},");
                sb.AppendLine($"            MaxRetentionPeriod = TimeSpan.FromHours({_configSettings.MaxRetentionHours:0.##}),");
                sb.AppendLine($"            StorageType = StorageType.{_configSettings.StorageType},");
            }

            sb.AppendLine($"            EnableFloatingIcon = {(_configSettings.EnableFloatingIcon ? "true" : "false")},");
            if (_configSettings.EnableFloatingIcon)
            {
                sb.AppendLine($"            KeepFloatingIconOnAppDeactivate = {(_configSettings.KeepFloatingIconOnAppDeactivate ? "true" : "false")},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        // 2. Initialize the Notifier engine (call once at application startup)");
            sb.AppendLine("        Notifier.Initialize(config);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    // STEP 3: Show Notifications anywhere from any thread");
            sb.AppendLine("    private void OnSaveButtonClick()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Thread-safe! Can be called from UI or background worker threads");
            sb.AppendLine("        Notifier.ShowSuccess(\"File Saved\", \"Your changes were saved successfully.\");");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    // STEP 4: Safe Teardown when Form Closes");
            sb.AppendLine("    protected override void OnFormClosing(FormClosingEventArgs e)");
            sb.AppendLine("    {");
            sb.AppendLine("        base.OnFormClosing(e);");
            sb.AppendLine("        // Releases all timers, active toast forms, floating icons, and GDI resources");
            sb.AppendLine("        Notifier.Dispose();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GenerateBasicSnippets()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("// Basic Notification API (Zero-Boilerplate, Thread-Safe)");
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("using Notifier;");
            sb.AppendLine();
            sb.AppendLine("// Success Notification");
            sb.AppendLine("Notifier.ShowSuccess(\"Order Created\", \"Order #40921 has been placed successfully.\");");
            sb.AppendLine();
            sb.AppendLine("// Info Notification");
            sb.AppendLine("Notifier.ShowInfo(\"Sync Complete\", \"Database synchronizer finished processing 124 records.\");");
            sb.AppendLine();
            sb.AppendLine("// Warning Notification");
            sb.AppendLine("Notifier.ShowWarning(\"Network Latency\", \"High round-trip latency detected on primary gateway.\");");
            sb.AppendLine();
            sb.AppendLine("// Error Notification");
            sb.AppendLine("Notifier.ShowError(\"Payment Failed\", \"Card authorization declined by issuing bank (Code: 51).\");");
            sb.AppendLine();
            sb.AppendLine("// Open History Notification Center");
            sb.AppendLine("Notifier.OpenNotificationCenter();");
            sb.AppendLine();
            sb.AppendLine("// Query Unread Count");
            sb.AppendLine("int unread = Notifier.UnreadCount;");

            return sb.ToString();
        }

        private static string GenerateTeardownSnippet()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("// Safe Deadlock-Free Teardown & Disposal");
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("using Notifier;");
            sb.AppendLine();
            sb.AppendLine("// In your Main Form's OnFormClosing or FormClosing event:");
            sb.AppendLine("protected override void OnFormClosing(FormClosingEventArgs e)");
            sb.AppendLine("{");
            sb.AppendLine("    base.OnFormClosing(e);");
            sb.AppendLine();
            sb.AppendLine("    // Completely shuts down background timers, active toasts,");
            sb.AppendLine("    // floating icons, and releases all GDI resources deterministically.");
            sb.AppendLine("    Notifier.Dispose();");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void OnCopyClick(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_tbCode.Text))
            {
                Clipboard.SetText(_tbCode.Text);
                _lblCopyNotice.Visible = true;

                var timer = new Timer { Interval = 2500 };
                timer.Tick += (s, args) =>
                {
                    _lblCopyNotice.Visible = false;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void OnSaveToFileClick(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "C# Source File (*.cs)|*.cs|Text File (*.txt)|*.txt",
                FileName = "NotifierIntegration.cs",
                Title = "Save Integration Code"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, _tbCode.Text, Encoding.UTF8);
                    MessageBox.Show(this, "File saved successfully!", "Code Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to save file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
