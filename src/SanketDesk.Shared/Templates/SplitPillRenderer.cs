#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using SanketDesk.UI;

namespace SanketDesk.Templates
{
    /// <summary>
    /// Split two-tone template featuring a solid status accent column on the left containing the icon,
    /// and a clean white card body on the right with title, message, and close button.
    /// </summary>
    internal sealed class SplitPillRenderer : INotificationTemplateRenderer
    {
        public string TemplateName => NotificationTemplateNames.SplitPill;

        public Size MeasureSize(Graphics g, NotificationModel model, NotificationLayoutSettings layout)
        {
            int width = layout.Scale(layout.DefaultWidth);
            int minHeight = layout.Scale(68);
            int leftBlockWidth = layout.Scale(56);
            int padding = layout.Scale(layout.Padding);
            int closeSize = layout.Scale(18);

            int contentWidth = width - leftBlockWidth - (padding * 2) - closeSize;

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            SizeF titleSize = g.MeasureString(model.GetFormattedTitle(), titleFont, contentWidth);
            SizeF msgSize = g.MeasureString(model.Message, msgFont, contentWidth);

            int calculatedHeight = padding + (int)titleSize.Height + layout.Scale(3) + (int)msgSize.Height + padding;

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

            int leftBlockWidth = layout.Scale(56);
            int padding = layout.Scale(layout.Padding);
            int cornerR = layout.Scale(layout.CornerRadius);
            int closeSize = layout.Scale(18);

            Color accent = TemplateColors.GetAccentColor(model.Type);

            // 1. Draw Outer Card with Rounded Corners
            Rectangle cardRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
            using (var path = CreateRoundedRectanglePath(cardRect, cornerR))
            {
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(whiteBrush, path);
                }
                using (var borderPen = new Pen(TemplateColors.CardBorder, 1.0f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Draw Left Solid Colored Column
            Rectangle leftColumnRect = new Rectangle(bounds.Left, bounds.Top, leftBlockWidth, bounds.Height);
            using (var leftClip = new GraphicsPath())
            {
                int d = cornerR * 2;
                leftClip.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
                leftClip.AddLine(bounds.Left + leftBlockWidth, bounds.Top, bounds.Left + leftBlockWidth, bounds.Bottom);
                leftClip.AddLine(bounds.Left + leftBlockWidth, bounds.Bottom, bounds.Left + cornerR, bounds.Bottom);
                leftClip.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
                leftClip.CloseFigure();

                using (var accentBrush = new SolidBrush(accent))
                {
                    g.FillPath(accentBrush, leftClip);
                }
            }

            // 3. Draw Icon in Left Column
            int iconSize = layout.Scale(26);
            Rectangle iconRect = new Rectangle(
                bounds.Left + (leftBlockWidth - iconSize) / 2,
                bounds.Top + (bounds.Height - iconSize) / 2,
                iconSize,
                iconSize);

            if (model.Icon != null)
            {
                g.DrawImage(model.Icon, iconRect);
            }
            else
            {
                VectorIconRenderer.DrawNotificationTypeIcon(g, iconRect, model.Type);
            }

            // 4. Draw Close Button
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize, bounds.Top + padding, closeSize, closeSize);
            bool isCloseHovered = state.HoveredArea == NotificationHitArea.CloseButton;
            VectorIconRenderer.DrawCloseButton(g, closeRect, isCloseHovered);

            // 5. Draw Title and Message
            int textLeft = bounds.Left + leftBlockWidth + padding;
            int textWidth = closeRect.Left - textLeft - layout.Scale(8);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            using (var titleBrush = new SolidBrush(TemplateColors.TextPrimary))
            {
                g.DrawString(model.GetFormattedTitle(), titleFont, titleBrush, new RectangleF(textLeft, bounds.Top + padding, textWidth, layout.Scale(20)));
            }

            using (var msgBrush = new SolidBrush(TemplateColors.TextSecondary))
            {
                int msgTop = bounds.Top + padding + layout.Scale(22);
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
