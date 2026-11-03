#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Notifier.UI;

namespace Notifier.Templates
{
    /// <summary>
    /// Windows 10/11 Fluent style template featuring modern acrylic card styling,
    /// app header watermark, subtle borders, clean Segoe UI typography, and bottom action buttons.
    /// </summary>
    internal sealed class WindowsFluentRenderer : INotificationTemplateRenderer
    {
        public string TemplateName => NotificationTemplateNames.WindowsFluent;

        private static readonly Color FluentDarkBg = Color.FromArgb(32, 32, 32);
        private static readonly Color FluentBorder = Color.FromArgb(60, 60, 60);
        private static readonly Color FluentButtonBg = Color.FromArgb(45, 45, 45);
        private static readonly Color FluentButtonHoverBg = Color.FromArgb(60, 60, 60);

        public Size MeasureSize(Graphics g, NotificationModel model, NotificationLayoutSettings layout)
        {
            int width = layout.Scale(360);
            int minHeight = layout.Scale(90);
            int padding = layout.Scale(12);

            int contentWidth = width - (padding * 2);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            SizeF titleSize = g.MeasureString(model.GetFormattedTitle(), titleFont, contentWidth);
            SizeF msgSize = g.MeasureString(model.Message, msgFont, contentWidth);

            int actionHeight = (model.Actions != null && model.Actions.Count > 0) ? layout.Scale(36) : 0;
            int calculatedHeight = padding + layout.Scale(20) + (int)titleSize.Height + layout.Scale(4) + (int)msgSize.Height + actionHeight + padding;

            if (layout.TextOverflowMode == TextOverflowMode.AutoHeight)
            {
                int maxHeight = layout.Scale(layout.MaxHeight + 50);
                calculatedHeight = Math.Min(maxHeight, Math.Max(minHeight, calculatedHeight));
            }
            else
            {
                calculatedHeight = minHeight + actionHeight;
            }

            return new Size(width, calculatedHeight);
        }

        public void Render(Graphics g, Rectangle bounds, NotificationModel model, NotificationVisualState state, NotificationLayoutSettings layout)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int padding = layout.Scale(12);
            int cornerR = layout.Scale(6);

            // 1. Draw Acrylic Dark Card Background & Subtle Border
            Rectangle cardRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
            using (var path = CreateRoundedRectanglePath(cardRect, cornerR))
            {
                using (var bgBrush = new SolidBrush(FluentDarkBg))
                {
                    g.FillPath(bgBrush, path);
                }
                using (var borderPen = new Pen(FluentBorder, 1.0f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Header: App badge or Type name + Close Button
            string headerText = !string.IsNullOrEmpty(model.CustomTypeBadge)
                ? model.CustomTypeBadge!
                : model.Type.ToString();

            Font headerFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(8.5f), FontStyle.Regular);
            using (var headerBrush = new SolidBrush(Color.FromArgb(180, 180, 180)))
            {
                g.DrawString(headerText, headerFont, headerBrush, new PointF(bounds.Left + padding, bounds.Top + padding - 2));
            }

            int closeSize = layout.Scale(16);
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize, bounds.Top + padding - 2, closeSize, closeSize);
            bool isCloseHovered = state.HoveredArea == NotificationHitArea.CloseButton;
            if (isCloseHovered)
            {
                using var hoverBrush = new SolidBrush(Color.FromArgb(50, 255, 255, 255));
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

            // 3. Title & Message in Crisp White
            int contentTop = bounds.Top + padding + layout.Scale(20);
            int contentWidth = bounds.Width - (padding * 2);

            Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(10.0f), FontStyle.Bold);
            Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.DrawString(model.GetFormattedTitle(), titleFont, whiteBrush, new RectangleF(bounds.Left + padding, contentTop, contentWidth, layout.Scale(20)));
            }

            using (var msgBrush = new SolidBrush(Color.FromArgb(210, 210, 210)))
            {
                int msgTop = contentTop + layout.Scale(22);
                int msgHeight = bounds.Height - msgTop - padding;
                if (model.Actions != null && model.Actions.Count > 0)
                {
                    msgHeight -= layout.Scale(36);
                }

                var sf = new StringFormat
                {
                    Trimming = layout.TextOverflowMode == TextOverflowMode.Ellipsis ? StringTrimming.EllipsisWord : StringTrimming.None,
                    FormatFlags = StringFormatFlags.LineLimit
                };
                g.DrawString(model.Message, msgFont, msgBrush, new RectangleF(bounds.Left + padding, msgTop, contentWidth, msgHeight), sf);
            }

            // 4. Bottom Action Buttons (Full width distribution)
            if (model.Actions != null && model.Actions.Count > 0)
            {
                int btnHeight = layout.Scale(30);
                int btnTop = bounds.Bottom - padding - btnHeight;
                int count = model.Actions.Count;
                int spacing = layout.Scale(8);
                int totalSpacing = spacing * (count - 1);
                int btnWidth = (contentWidth - totalSpacing) / count;

                Font btnFont = GdiResourceCache.Instance.GetFont("Segoe UI", layout.Scale(9.0f), FontStyle.Regular);

                for (int i = 0; i < count; i++)
                {
                    int btnLeft = bounds.Left + padding + i * (btnWidth + spacing);
                    Rectangle btnRect = new Rectangle(btnLeft, btnTop, btnWidth, btnHeight);
                    bool isHovered = state.HoveredArea == NotificationHitArea.ActionButton && state.HoveredActionIndex == i;

                    using (var btnPath = CreateRoundedRectanglePath(btnRect, layout.Scale(4)))
                    {
                        using (var btnBg = new SolidBrush(isHovered ? FluentButtonHoverBg : FluentButtonBg))
                        {
                            g.FillPath(btnBg, btnPath);
                        }
                    }

                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(model.Actions[i].Title, btnFont, textBrush, btnRect, sf);
                    }
                }
            }
        }

        public NotificationHitTestResult HitTest(Point location, Rectangle bounds, NotificationModel model, NotificationLayoutSettings layout)
        {
            int padding = layout.Scale(12);
            int closeSize = layout.Scale(16);
            Rectangle closeRect = new Rectangle(bounds.Right - padding - closeSize - 2, bounds.Top + padding - 4, closeSize + 4, closeSize + 4);

            if (closeRect.Contains(location))
            {
                return new NotificationHitTestResult(NotificationHitArea.CloseButton, -1, closeRect);
            }

            // Hit test bottom action buttons
            if (model.Actions != null && model.Actions.Count > 0)
            {
                int btnHeight = layout.Scale(30);
                int btnTop = bounds.Bottom - padding - btnHeight;
                int count = model.Actions.Count;
                int spacing = layout.Scale(8);
                int contentWidth = bounds.Width - (padding * 2);
                int btnWidth = (contentWidth - (spacing * (count - 1))) / count;

                for (int i = 0; i < count; i++)
                {
                    int btnLeft = bounds.Left + padding + i * (btnWidth + spacing);
                    Rectangle btnRect = new Rectangle(btnLeft, btnTop, btnWidth, btnHeight);
                    if (btnRect.Contains(location))
                    {
                        return new NotificationHitTestResult(NotificationHitArea.ActionButton, i, btnRect);
                    }
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
