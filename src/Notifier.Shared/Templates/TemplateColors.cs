#nullable enable
using System.Drawing;

namespace Notifier.Templates
{
    /// <summary>
    /// Tailored color palettes for notification types and templates.
    /// Uses harmonious, modern, high-contrast tones.
    /// </summary>
    internal static class TemplateColors
    {
        public static Color SuccessAccent = Color.FromArgb(40, 167, 69);      // Modern Emerald Green
        public static Color InfoAccent = Color.FromArgb(23, 162, 184);         // Modern Cyan / Sky Blue
        public static Color WarningAccent = Color.FromArgb(255, 179, 0);       // Modern Amber
        public static Color ErrorAccent = Color.FromArgb(220, 53, 69);         // Modern Crimson Red
        public static Color CustomAccent = Color.FromArgb(111, 66, 193);       // Modern Purple

        public static Color SuccessBg = Color.FromArgb(240, 249, 244);
        public static Color InfoBg = Color.FromArgb(240, 248, 255);
        public static Color WarningBg = Color.FromArgb(255, 251, 235);
        public static Color ErrorBg = Color.FromArgb(254, 242, 242);

        public static Color CardBackground = Color.FromArgb(255, 255, 255);
        public static Color CardBorder = Color.FromArgb(229, 231, 235);
        public static Color CardShadow = Color.FromArgb(20, 0, 0, 0);

        public static Color TextPrimary = Color.FromArgb(31, 41, 55);         // Slate 800
        public static Color TextSecondary = Color.FromArgb(107, 114, 128);     // Slate 500
        public static Color TextMuted = Color.FromArgb(156, 163, 175);         // Slate 400

        public static Color GetAccentColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return SuccessAccent;
                case NotificationType.Info: return InfoAccent;
                case NotificationType.Warning: return WarningAccent;
                case NotificationType.Error: return ErrorAccent;
                case NotificationType.Custom:
                default: return CustomAccent;
            }
        }

        public static Color GetSoftBackground(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return SuccessBg;
                case NotificationType.Info: return InfoBg;
                case NotificationType.Warning: return WarningBg;
                case NotificationType.Error: return ErrorBg;
                case NotificationType.Custom:
                default: return Color.FromArgb(245, 243, 255);
            }
        }
    }
}
