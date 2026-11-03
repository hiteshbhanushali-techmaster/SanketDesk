#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using SanketDesk.UI;

namespace SanketDesk.Templates
{
    /// <summary>
    /// Solid accent template featuring a full vibrant status background color,
    /// crisp white typography, high-contrast vector icons, and an integrated progress bar.
    /// </summary>
    internal sealed class SolidAccentRenderer : INotificationTemplateRenderer
    {
        public string TemplateName => NotificationTemplateNames.SolidAccent;

        public Size MeasureSize(Graphics g, NotificationModel model, NotificationLayoutSettings layout)
        {
            int width = layout.Scale(layout.DefaultWidth);
            int minHeight = layout.Scale(70);
            int padding = layout.Scale(layout.Padding);
            int iconSize = layout.Scale(26);
            int closeSize = layout.Scale(20);

            int contentWidth = width - (padding * 2) - iconSize - closeSize - layout.Scale(16);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            SizeF titleSize = g.MeasureString(model.GetFormattedTitle(), titleFont, contentWidth);
            SizeF msgSize = g.MeasureString(model.Message, msgFont, contentWidth);

            int calculatedHeight = padding + (int)titleSize.Height + layout.Scale(4) + (int)msgSize.Height + padding + layout.Scale(4);

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
            int closeSize = layout.Scale(20);

            Color accent = TemplateColors.GetAccentColor(model.Type);

            // 1. Draw Vibrant Solid Background with Rounded Corners
            Rectangle cardRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
            using (var path = CreateRoundedRectanglePath(cardRect, cornerR))
            {
                using (var bgBrush = new SolidBrush(accent))
                {
                    g.FillPath(bgBrush, path);
                }
            }

            // 2. Draw Icon (or white circular outline vector icon)
            Rectangle iconRect = new Rectangle(bounds.Left + padding, bounds.Top + padding, iconSize, iconSize);
            if (model.Icon != null)
            {
                g.DrawImage(model.Icon, iconRect);
            }
            else
            {
                VectorIconRenderer.DrawNotificationTypeIcon(g, iconRect, model.Type);
            }

            // 3. Draw Close Button (White translucent)
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize, bounds.Top + padding, closeSize, closeSize);
            bool isCloseHovered = state.HoveredArea == NotificationHitArea.CloseButton;
            if (isCloseHovered)
            {
                using var hoverBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
                g.FillEllipse(hoverBrush, closeRect);
            }
            using (var pen = new Pen(Color.White, closeSize * 0.12f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float inset = closeSize * 0.3f;
                g.DrawLine(pen, closeRect.Left + inset, closeRect.Top + inset, closeRect.Right - inset, closeRect.Bottom - inset);
                g.DrawLine(pen, closeRect.Right - inset, closeRect.Top + inset, closeRect.Left + inset, closeRect.Bottom - inset);
            }

            // 4. Draw Title and Message in Crisp White
            int textLeft = iconRect.Right + layout.Scale(12);
            int textWidth = closeRect.Left - textLeft - layout.Scale(8);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.DrawString(model.GetFormattedTitle(), titleFont, whiteBrush, new RectangleF(textLeft, bounds.Top + padding, textWidth, layout.Scale(20)));
            }

            using (var lightWhiteBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            {
                int msgTop = bounds.Top + padding + layout.Scale(22);
                int msgHeight = bounds.Height - msgTop - padding - layout.Scale(4);
                var sf = new StringFormat
                {
                    Trimming = layout.TextOverflowMode == TextOverflowMode.Ellipsis ? StringTrimming.EllipsisWord : StringTrimming.None,
                    FormatFlags = StringFormatFlags.LineLimit
                };
                g.DrawString(model.Message, msgFont, lightWhiteBrush, new RectangleF(textLeft, msgTop, textWidth, msgHeight), sf);
            }

            // 5. Draw Bottom Timeout Line (translucent white)
            double ratio = Math.Max(0.0, Math.Min(1.0, state.RemainingDurationRatio));
            if (ratio > 0.0)
            {
                int barHeight = layout.Scale(3);
                int barWidth = (int)((bounds.Width - 2) * ratio);
                Rectangle progressRect = new Rectangle(bounds.Left + 1, bounds.Bottom - barHeight - 1, barWidth, barHeight);
                using (var progressBrush = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                {
                    g.FillRectangle(progressBrush, progressRect);
                }
            }
        }

        public NotificationHitTestResult HitTest(Point location, Rectangle bounds, NotificationModel model, NotificationLayoutSettings layout)
        {
            int padding = layout.Scale(layout.Padding);
            int closeSize = layout.Scale(20);
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize - 2, bounds.Top + padding - 2, closeSize + 4, closeSize + 4);

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
