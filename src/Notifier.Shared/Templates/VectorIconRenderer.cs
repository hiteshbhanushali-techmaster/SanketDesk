#nullable enable
using System.Drawing;
using System.Drawing.Drawing2D;
using Notifier.UI;

namespace Notifier.Templates
{
    /// <summary>
    /// Draws resolution-independent vector icons for notification types and interactive controls.
    /// Anti-aliased and pixel-perfect across all DPI scaling levels (100% to 200%+).
    /// </summary>
    internal static class VectorIconRenderer
    {
        public static void DrawNotificationTypeIcon(Graphics g, Rectangle bounds, NotificationType type)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color accent = TemplateColors.GetAccentColor(type);
            var cache = GdiResourceCache.Instance;

            // Draw circular background badge
            using (var brush = new SolidBrush(accent))
            {
                g.FillEllipse(brush, bounds);
            }

            // Draw inner glyph
            using (var whitePen = new Pen(Color.White, bounds.Width * 0.12f))
            {
                whitePen.StartCap = LineCap.Round;
                whitePen.EndCap = LineCap.Round;

                switch (type)
                {
                    case NotificationType.Success:
                        // Draw checkmark
                        PointF p1 = new PointF(bounds.Left + bounds.Width * 0.28f, bounds.Top + bounds.Height * 0.52f);
                        PointF p2 = new PointF(bounds.Left + bounds.Width * 0.44f, bounds.Top + bounds.Height * 0.68f);
                        PointF p3 = new PointF(bounds.Left + bounds.Width * 0.72f, bounds.Top + bounds.Height * 0.34f);
                        g.DrawLines(whitePen, new[] { p1, p2, p3 });
                        break;

                    case NotificationType.Info:
                        // Draw info dot and line
                        using (var dotBrush = new SolidBrush(Color.White))
                        {
                            float dotRadius = bounds.Width * 0.08f;
                            g.FillEllipse(dotBrush, bounds.Left + bounds.Width * 0.5f - dotRadius, bounds.Top + bounds.Height * 0.28f, dotRadius * 2, dotRadius * 2);
                        }
                        g.DrawLine(whitePen,
                            bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.44f,
                            bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.72f);
                        break;

                    case NotificationType.Warning:
                        // Draw exclamation dot and line
                        g.DrawLine(whitePen,
                            bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.28f,
                            bounds.Left + bounds.Width * 0.5f, bounds.Top + bounds.Height * 0.56f);
                        using (var dotBrush = new SolidBrush(Color.White))
                        {
                            float dotRadius = bounds.Width * 0.08f;
                            g.FillEllipse(dotBrush, bounds.Left + bounds.Width * 0.5f - dotRadius, bounds.Top + bounds.Height * 0.68f, dotRadius * 2, dotRadius * 2);
                        }
                        break;

                    case NotificationType.Error:
                        // Draw 'X'
                        float inset = bounds.Width * 0.3f;
                        g.DrawLine(whitePen, bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
                        g.DrawLine(whitePen, bounds.Right - inset, bounds.Top + inset, bounds.Left + inset, bounds.Bottom - inset);
                        break;

                    case NotificationType.Custom:
                    default:
                        // Draw star / asterisk
                        float midX = bounds.Left + bounds.Width * 0.5f;
                        float midY = bounds.Top + bounds.Height * 0.5f;
                        float r = bounds.Width * 0.22f;
                        g.DrawLine(whitePen, midX, midY - r, midX, midY + r);
                        g.DrawLine(whitePen, midX - r, midY, midX + r, midY);
                        break;
                }
            }
        }

        public static void DrawCloseButton(Graphics g, Rectangle bounds, bool isHovered)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (isHovered)
            {
                using var hoverBg = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
                g.FillEllipse(hoverBg, bounds);
            }

            Color xColor = isHovered ? TemplateColors.TextPrimary : TemplateColors.TextMuted;
            using var pen = new Pen(xColor, bounds.Width * 0.12f);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;

            float inset = bounds.Width * 0.3f;
            g.DrawLine(pen, bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
            g.DrawLine(pen, bounds.Right - inset, bounds.Top + inset, bounds.Left + inset, bounds.Bottom - inset);
        }

        public static void DrawBellIcon(Graphics g, Rectangle bounds, Color bellColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var brush = new SolidBrush(bellColor);
            using var pen = new Pen(bellColor, bounds.Width * 0.08f);

            // Bell dome and skirt
            float cx = bounds.Left + bounds.Width * 0.5f;
            float topY = bounds.Top + bounds.Height * 0.25f;
            float botY = bounds.Top + bounds.Height * 0.70f;
            float w = bounds.Width * 0.35f;

            using (var path = new GraphicsPath())
            {
                path.AddArc(cx - w * 0.5f, topY, w, w, 180, 180);
                path.AddLine(cx + w * 0.5f, topY + w * 0.5f, cx + w * 0.75f, botY);
                path.AddLine(cx + w * 0.75f, botY, cx - w * 0.75f, botY);
                path.AddLine(cx - w * 0.75f, botY, cx - w * 0.5f, topY + w * 0.5f);
                path.CloseFigure();
                g.FillPath(brush, path);
            }

            // Bell clapper
            float clapperR = bounds.Width * 0.08f;
            g.FillEllipse(brush, cx - clapperR, botY + 1, clapperR * 2, clapperR * 2);
        }
    }
}
