#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using SanketDesk;

namespace ConfiguratorSample.Models
{
    public enum ActionButtonPreset
    {
        None = 0,
        UndoAction = 1,
        AcceptDecline = 2,
        AcknowledgeSnooze = 3
    }

    /// <summary>
    /// Editable options model for triggering a custom toast from the PropertyGrid.
    /// </summary>
    public sealed class ToastCustomOptions
    {
        [Category("1. Content")]
        [DisplayName("Title")]
        [Description("Headline text displayed at the top of the toast card.")]
        [DefaultValue("Order Processed")]
        public string Title { get; set; } = "Order Processed";

        [Category("1. Content")]
        [DisplayName("Message Body")]
        [Description("Detailed description or summary text in the toast body.")]
        [DefaultValue("Invoice #INV-2026-88 has been generated and queued for dispatch.")]
        public string Message { get; set; } = "Invoice #INV-2026-88 has been generated and queued for dispatch.";

        [Category("1. Content")]
        [DisplayName("Notification Type")]
        [Description("Type: Success, Info, Warning, Error, or Custom.")]
        [DefaultValue(NotificationType.Success)]
        public NotificationType Type { get; set; } = NotificationType.Success;

        [Category("1. Content")]
        [DisplayName("Custom Type Badge")]
        [Description("Custom label shown when NotificationType is Custom (e.g. 'BILLING', 'SECURITY').")]
        [DefaultValue("")]
        public string CustomTypeBadge { get; set; } = "";

        [Category("2. Appearance Override")]
        [DisplayName("Template Override")]
        [Description("Override default template for this specific toast (or leave blank to use engine default).")]
        [TypeConverter(typeof(TemplateNameConverter))]
        [DefaultValue(NotificationTemplateNames.ModernCard)]
        public string Template { get; set; } = NotificationTemplateNames.ModernCard;

        [Category("2. Appearance Override")]
        [DisplayName("Duration (seconds)")]
        [Description("Visible display duration in seconds for this toast.")]
        [DefaultValue(6.0)]
        public double DurationSeconds { get; set; } = 6.0;

        [Category("2. Appearance Override")]
        [DisplayName("Animation Override")]
        [Description("Appearance and dismissal transition animation override.")]
        [DefaultValue(AnimationMode.None)]
        public AnimationMode Animation { get; set; } = AnimationMode.None;

        [Category("2. Appearance Override")]
        [DisplayName("Text Overflow")]
        [Description("Text layout override: Ellipsis or AutoHeight.")]
        [DefaultValue(TextOverflowMode.Ellipsis)]
        public TextOverflowMode TextOverflow { get; set; } = TextOverflowMode.Ellipsis;

        [Category("3. Ingestion & Priority")]
        [DisplayName("Explicit Priority")]
        [Description("Explicit priority value. SystemDefault resolves based on NotificationType.")]
        [DefaultValue(NotificationPriority.SystemDefault)]
        public NotificationPriority Priority { get; set; } = NotificationPriority.SystemDefault;

        [Category("3. Ingestion & Priority")]
        [DisplayName("Group Key")]
        [Description("Deterministic grouping key for burst clubbing and coalesce. Leave empty for none.")]
        [DefaultValue("")]
        public string GroupKey { get; set; } = "";

        [Category("4. Interactive Actions")]
        [DisplayName("Action Buttons Preset")]
        [Description("Preset interactive buttons rendered directly on the toast card.")]
        [DefaultValue(ActionButtonPreset.None)]
        public ActionButtonPreset ActionPreset { get; set; } = ActionButtonPreset.None;

        /// <summary>
        /// Converts the current options into an <see cref="AdvancedNotificationOptions"/> instance.
        /// </summary>
        public AdvancedNotificationOptions ToAdvancedNotificationOptions(Action<string>? statusCallback = null)
        {
            var options = new AdvancedNotificationOptions
            {
                Type = Type,
                Title = Title,
                Message = Message,
                CustomTypeBadge = !string.IsNullOrWhiteSpace(CustomTypeBadge) ? CustomTypeBadge : null,
                Template = !string.IsNullOrWhiteSpace(Template) ? Template : null,
                Priority = Priority != NotificationPriority.SystemDefault ? Priority : null,
                GroupKey = !string.IsNullOrWhiteSpace(GroupKey) ? GroupKey : null,
                Duration = TimeSpan.FromSeconds(Math.Max(0.5, DurationSeconds)),
                Animation = Animation != AnimationMode.None ? Animation : (AnimationMode?)null,
                TextOverflow = TextOverflow,
                OnClick = id => statusCallback?.Invoke($"Toast #{id} clicked by user."),
                OnDoubleClick = id => statusCallback?.Invoke($"Toast #{id} double-clicked by user."),
                OnClose = id => statusCallback?.Invoke($"Toast #{id} closed/dismissed.")
            };

            switch (ActionPreset)
            {
                case ActionButtonPreset.UndoAction:
                    options.Actions = new[]
                    {
                        new NotificationAction("Undo", id =>
                        {
                            statusCallback?.Invoke($"Undo clicked on toast #{id}!");
                            SanketDeskNotifier.ShowSuccess("Action Undone", "The operation was rolled back successfully.");
                        }),
                        new NotificationAction("Dismiss", id =>
                        {
                            statusCallback?.Invoke($"Dismiss clicked on toast #{id}.");
                        })
                    };
                    break;

                case ActionButtonPreset.AcceptDecline:
                    options.Actions = new[]
                    {
                        new NotificationAction("Accept", id =>
                        {
                            statusCallback?.Invoke($"Accepted toast #{id}.");
                            SanketDeskNotifier.ShowSuccess("Accepted", "Request confirmed.");
                        }),
                        new NotificationAction("Decline", id =>
                        {
                            statusCallback?.Invoke($"Declined toast #{id}.");
                            SanketDeskNotifier.ShowWarning("Declined", "Request was rejected.");
                        })
                    };
                    break;

                case ActionButtonPreset.AcknowledgeSnooze:
                    options.Actions = new[]
                    {
                        new NotificationAction("Acknowledge", id =>
                        {
                            statusCallback?.Invoke($"Acknowledged toast #{id}.");
                        }),
                        new NotificationAction("Snooze (5m)", id =>
                        {
                            statusCallback?.Invoke($"Snoozed toast #{id}.");
                            SanketDeskNotifier.ShowInfo("Snoozed", "Alert snoozed for 5 minutes.");
                        })
                    };
                    break;
            }

            return options;
        }

        /// <summary>
        /// Generates the C# snippet for submitting this notification.
        /// </summary>
        public string GenerateCSharpSubmissionCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("// Trigger Toast Notification");
            sb.AppendLine("// -------------------------------------------------------------");
            sb.AppendLine("using SanketDesk;");
            sb.AppendLine();

            if (ActionPreset == ActionButtonPreset.None &&
                string.IsNullOrWhiteSpace(GroupKey) &&
                string.IsNullOrWhiteSpace(CustomTypeBadge) &&
                Priority == NotificationPriority.SystemDefault &&
                Animation == AnimationMode.None)
            {
                // Simple API is cleaner
                string methodName = Type switch
                {
                    NotificationType.Success => "ShowSuccess",
                    NotificationType.Info => "ShowInfo",
                    NotificationType.Warning => "ShowWarning",
                    NotificationType.Error => "ShowError",
                    _ => "Show"
                };

                if (methodName != "Show")
                {
                    sb.AppendLine($"Notifier.{methodName}(");
                    sb.AppendLine($"    \"{Title}\",");
                    sb.AppendLine($"    \"{Message}\");");
                    return sb.ToString();
                }
            }

            sb.AppendLine("Notifier.Show(new AdvancedNotificationOptions");
            sb.AppendLine("{");
            sb.AppendLine($"    Type = NotificationType.{Type},");
            sb.AppendLine($"    Title = \"{Title}\",");
            sb.AppendLine($"    Message = \"{Message}\",");

            if (!string.IsNullOrWhiteSpace(Template))
            {
                sb.AppendLine($"    Template = NotificationTemplateNames.{Template},");
            }

            if (!string.IsNullOrWhiteSpace(CustomTypeBadge))
            {
                sb.AppendLine($"    CustomTypeBadge = \"{CustomTypeBadge}\",");
            }

            if (Priority != NotificationPriority.SystemDefault)
            {
                sb.AppendLine($"    Priority = NotificationPriority.{Priority},");
            }

            if (!string.IsNullOrWhiteSpace(GroupKey))
            {
                sb.AppendLine($"    GroupKey = \"{GroupKey}\",");
            }

            sb.AppendLine($"    Duration = TimeSpan.FromSeconds({DurationSeconds:0.##}),");

            if (Animation != AnimationMode.None)
            {
                sb.AppendLine($"    Animation = AnimationMode.{Animation},");
            }

            if (TextOverflow != TextOverflowMode.Ellipsis)
            {
                sb.AppendLine($"    TextOverflow = TextOverflowMode.{TextOverflow},");
            }

            if (ActionPreset == ActionButtonPreset.UndoAction)
            {
                sb.AppendLine("    Actions = new[]");
                sb.AppendLine("    {");
                sb.AppendLine("        new NotificationAction(\"Undo\", id => Notifier.ShowSuccess(\"Undone\", \"Action was rolled back.\")),");
                sb.AppendLine("        new NotificationAction(\"Dismiss\", id => { })");
                sb.AppendLine("    },");
            }
            else if (ActionPreset == ActionButtonPreset.AcceptDecline)
            {
                sb.AppendLine("    Actions = new[]");
                sb.AppendLine("    {");
                sb.AppendLine("        new NotificationAction(\"Accept\", id => Notifier.ShowSuccess(\"Accepted\", \"Action confirmed.\")),");
                sb.AppendLine("        new NotificationAction(\"Decline\", id => Notifier.ShowWarning(\"Declined\", \"Action cancelled.\"))");
                sb.AppendLine("    },");
            }

            sb.AppendLine("    OnClick = id => Console.WriteLine($\"Notification {id} clicked\")");
            sb.AppendLine("});");

            return sb.ToString();
        }
    }
}
