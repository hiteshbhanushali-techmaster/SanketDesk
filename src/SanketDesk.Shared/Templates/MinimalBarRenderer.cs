#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using SanketDesk.UI;

namespace SanketDesk.Templates
{
    /// <summary>
    /// Minimalist bar template featuring a crisp vertical status accent stripe on the left edge,
    /// a compact horizontal profile, clean typography, and a close button.
    /// </summary>
    internal sealed class MinimalBarRenderer : INotificationTemplateRenderer
    {
        public string TemplateName => NotificationTemplateNames.MinimalBar;

        public Size MeasureSize(Graphics g, NotificationModel model, NotificationLayoutSettings layout)
        {
            int width = layout.Scale(layout.DefaultWidth);
            int minHeight = layout.Scale(60);
            int padding = layout.Scale(10);
            int iconSize = layout.Scale(22);
            int closeSize = layout.Scale(18);

            int contentWidth = width - (padding * 2) - iconSize - closeSize - layout.Scale(24);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.5f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(8.5f), FontStyle.Regular);

            SizeF titleSize = g.MeasureString(model.GetFormattedTitle(), titleFont, contentWidth);
            SizeF msgSize = g.MeasureString(model.Message, msgFont, contentWidth);

            int calculatedHeight = padding + (int)titleSize.Height + layout.Scale(2) + (int)msgSize.Height + padding;

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

            int padding = layout.Scale(10);
            int stripeWidth = layout.Scale(5);
            int iconSize = layout.Scale(22);
            int closeSize = layout.Scale(18);

            // 1. Draw Background and subtle border
            using (var bgBrush = new SolidBrush(TemplateColors.CardBackground))
            {
                g.FillRectangle(bgBrush, bounds);
            }
            using (var borderPen = new Pen(TemplateColors.CardBorder, 1.0f))
            {
                g.DrawRectangle(borderPen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
            }

            // 2. Draw Left Accent Stripe
            Color accent = TemplateColors.GetAccentColor(model.Type);
            using (var stripeBrush = new SolidBrush(accent))
            {
                g.FillRectangle(stripeBrush, bounds.Left, bounds.Top, stripeWidth, bounds.Height);
            }

            // 3. Draw Icon
            Rectangle iconRect = new Rectangle(bounds.Left + stripeWidth + padding, bounds.Top + (bounds.Height - iconSize) / 2, iconSize, iconSize);
            if (model.Icon != null)
            {
                g.DrawImage(model.Icon, iconRect);
            }
            else
            {
                VectorIconRenderer.DrawNotificationTypeIcon(g, iconRect, model.Type);
            }

            // 4. Draw Close Button
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize, bounds.Top + (bounds.Height - closeSize) / 2, closeSize, closeSize);
            bool isCloseHovered = state.HoveredArea == NotificationHitArea.CloseButton;
            VectorIconRenderer.DrawCloseButton(g, closeRect, isCloseHovered);

            // 5. Draw Title and Message
            int textLeft = iconRect.Right + layout.Scale(10);
            int textWidth = closeRect.Left - textLeft - layout.Scale(8);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.5f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(8.5f), FontStyle.Regular);

            using (var titleBrush = new SolidBrush(TemplateColors.TextPrimary))
            {
                g.DrawString(model.GetFormattedTitle(), titleFont, titleBrush, new RectangleF(textLeft, bounds.Top + padding, textWidth, layout.Scale(18)));
            }

            using (var msgBrush = new SolidBrush(TemplateColors.TextSecondary))
            {
                int msgTop = bounds.Top + padding + layout.Scale(18);
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
            int padding = layout.Scale(10);
            int closeSize = layout.Scale(18);
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize - 2, bounds.Top + (bounds.Height - closeSize) / 2 - 2, closeSize + 4, closeSize + 4);

            if (closeRect.Contains(location))
            {
                return new NotificationHitTestResult(NotificationHitArea.CloseButton, -1, closeRect);
            }

            if (bounds.Contains(location))
            {
                return new NotificationHitTestResult(NotificationHitArea.Body, -1, bounds);
            }

            return NotificationHitTestResult.None;
        }
    }
}
