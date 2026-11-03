#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Notifier.UI;

namespace Notifier.Templates
{
    /// <summary>
    /// Interactive action prompt template optimized for notifications with action buttons (e.g. "Undo", "Review", "Dismiss").
    /// Provides hover state highlights and precise hit-testing per button.
    /// </summary>
    internal sealed class ActionPromptRenderer : INotificationTemplateRenderer
    {
        public string TemplateName => NotificationTemplateNames.ActionPrompt;

        public Size MeasureSize(Graphics g, NotificationModel model, NotificationLayoutSettings layout)
        {
            int width = layout.Scale(layout.DefaultWidth + 30);
            int minHeight = layout.Scale(68);
            int padding = layout.Scale(layout.Padding);
            int iconSize = layout.Scale(26);
            int closeSize = layout.Scale(18);

            // Reserve space for action buttons on the right or bottom
            int buttonAreaWidth = 0;
            if (model.Actions != null && model.Actions.Count > 0)
            {
                Font btnFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Bold);
                for (int i = 0; i < model.Actions.Count; i++)
                {
                    SizeF bSize = g.MeasureString(model.Actions[i].Title, btnFont);
                    buttonAreaWidth += (int)bSize.Width + layout.Scale(24);
                }
            }

            int contentWidth = width - (padding * 2) - iconSize - closeSize - buttonAreaWidth - layout.Scale(16);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            SizeF titleSize = g.MeasureString(model.GetFormattedTitle(), titleFont, contentWidth);
            SizeF msgSize = g.MeasureString(model.Message, msgFont, contentWidth);

            int calculatedHeight = padding + (int)titleSize.Height + layout.Scale(4) + (int)msgSize.Height + padding;

            if (layout.TextOverflowMode == TextOverflowMode.AutoHeight)
            {
                int maxHeight = layout.Scale(layout.MaxHeight);
                calculatedHeight = Math.Min(maxHeight, Math.Max(minHeight, calculatedHeight));
            }
            else
            {
                calculatedHeight = minHeight;
            }

            return new Size(width, calculatedHeight);
        }

        public void Render(Graphics g, Rectangle bounds, NotificationModel model, NotificationVisualState state, NotificationLayoutSettings layout)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int padding = layout.Scale(layout.Padding);
            int cornerR = layout.Scale(layout.CornerRadius);
            int iconSize = layout.Scale(26);
            int closeSize = layout.Scale(18);

            // 1. Draw Card Background & Border
            Rectangle cardRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
            using (var path = CreateRoundedRectanglePath(cardRect, cornerR))
            {
                using (var bgBrush = new SolidBrush(TemplateColors.CardBackground))
                {
                    g.FillPath(bgBrush, path);
                }
                using (var borderPen = new Pen(TemplateColors.CardBorder, 1.0f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Draw Icon
            Rectangle iconRect = new Rectangle(bounds.Left + padding, bounds.Top + (bounds.Height - iconSize) / 2, iconSize, iconSize);
            if (model.Icon != null)
            {
                g.DrawImage(model.Icon, iconRect);
            }
            else
            {
                VectorIconRenderer.DrawNotificationTypeIcon(g, iconRect, model.Type);
            }

            // 3. Draw Close Button
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize, bounds.Top + (bounds.Height - closeSize) / 2, closeSize, closeSize);
            bool isCloseHovered = state.HoveredArea == NotificationHitArea.CloseButton;
            VectorIconRenderer.DrawCloseButton(g, closeRect, isCloseHovered);

            // 4. Calculate Action Buttons Area from Right Edge
            int curRight = closeRect.Left - layout.Scale(12);
            Font btnFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Bold);

            if (model.Actions != null && model.Actions.Count > 0)
            {
                for (int i = model.Actions.Count - 1; i >= 0; i--)
                {
                    NotificationAction action = model.Actions[i];
                    SizeF btnTextSize = g.MeasureString(action.Title, btnFont);
                    int btnWidth = (int)btnTextSize.Width + layout.Scale(16);
                    int btnHeight = layout.Scale(28);
                    Rectangle btnRect = new Rectangle(curRight - btnWidth, bounds.Top + (bounds.Height - btnHeight) / 2, btnWidth, btnHeight);

                    bool isBtnHovered = state.HoveredArea == NotificationHitArea.ActionButton && state.HoveredActionIndex == i;

                    using (var btnPath = CreateRoundedRectanglePath(btnRect, layout.Scale(4)))
                    {
                        if (isBtnHovered)
                        {
                            using var hoverBrush = new SolidBrush(Color.FromArgb(235, 240, 248));
                            g.FillPath(hoverBrush, btnPath);
                        }
                        using var btnBorder = new Pen(isBtnHovered ? TemplateColors.InfoAccent : TemplateColors.CardBorder, 1.0f);
                        g.DrawPath(btnBorder, btnPath);
                    }

                    Color btnTextColor = isBtnHovered ? TemplateColors.InfoAccent : TemplateColors.TextPrimary;
                    using (var textBrush = new SolidBrush(btnTextColor))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(action.Title, btnFont, textBrush, btnRect, sf);
                    }

                    curRight = btnRect.Left - layout.Scale(8);
                }
            }

            // 5. Draw Title and Message
            int textLeft = iconRect.Right + layout.Scale(12);
            int textWidth = curRight - textLeft - layout.Scale(8);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            using (var titleBrush = new SolidBrush(TemplateColors.TextPrimary))
            {
                g.DrawString(model.GetFormattedTitle(), titleFont, titleBrush, new RectangleF(textLeft, bounds.Top + padding, textWidth, layout.Scale(20)));
            }

            using (var msgBrush = new SolidBrush(TemplateColors.TextSecondary))
            {
                int msgTop = bounds.Top + padding + layout.Scale(20);
                int msgHeight = bounds.Height - msgTop - padding;
                var sf = new StringFormat
                {
                    Trimming = layout.TextOverflowMode == TextOverflowMode.Ellipsis ? StringTrimming.EllipsisWord : StringTrimming.None,
                    FormatFlags = StringFormatFlags.LineLimit
                };
                g.DrawString(model.Message, msgFont, msgBrush, new RectangleF(textLeft, msgTop, textWidth, msgHeight), sf);
            }
        }

        public NotificationHitTestResult HitTest(Point location, Rectangle bounds, NotificationModel model, NotificationLayoutSettings layout)
        {
            int padding = layout.Scale(layout.Padding);
            int closeSize = layout.Scale(18);
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize - 2, bounds.Top + (bounds.Height - closeSize) / 2 - 2, closeSize + 4, closeSize + 4);

            if (closeRect.Contains(location))
            {
                return new NotificationHitTestResult(NotificationHitArea.CloseButton, -1, closeRect);
            }

            // Hit test action buttons
            if (model.Actions != null && model.Actions.Count > 0)
            {
                int curRight = closeRect.Left - layout.Scale(12);
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                Font btnFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Bold);

                for (int i = model.Actions.Count - 1; i >= 0; i--)
                {
                    SizeF btnTextSize = g.MeasureString(model.Actions[i].Title, btnFont);
                    int btnWidth = (int)btnTextSize.Width + layout.Scale(16);
                    int btnHeight = layout.Scale(28);
                    Rectangle btnRect = new Rectangle(curRight - btnWidth, bounds.Top + (bounds.Height - btnHeight) / 2, btnWidth, btnHeight);

                    if (btnRect.Contains(location))
                    {
                        return new NotificationHitTestResult(NotificationHitArea.ActionButton, i, btnRect);
                    }

                    curRight = btnRect.Left - layout.Scale(8);
                }
            }

            if (bounds.Contains(location))
            {
                return new NotificationHitTestResult(NotificationHitArea.Body, -1, bounds);
            }

            return NotificationHitTestResult.None;
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
