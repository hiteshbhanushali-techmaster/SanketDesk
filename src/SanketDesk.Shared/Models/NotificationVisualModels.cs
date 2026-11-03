using System.Drawing;

namespace SanketDesk
{
    /// <summary>
    /// Result of hit-testing a point against a notification toast.
    /// </summary>
    public struct NotificationHitTestResult
    {
        public NotificationHitArea HitArea { get; }
        public int ActionIndex { get; }
        public Rectangle ElementBounds { get; }

        public static NotificationHitTestResult None => new NotificationHitTestResult(NotificationHitArea.None, -1, Rectangle.Empty);

        public NotificationHitTestResult(NotificationHitArea hitArea, int actionIndex = -1, Rectangle elementBounds = default)
        {
            HitArea = hitArea;
            ActionIndex = actionIndex;
            ElementBounds = elementBounds;
        }
    }

    /// <summary>
    /// Layout metrics and DPI scale settings used by renderers to measure and paint notifications.
    /// </summary>
    public sealed class NotificationLayoutSettings
    {
        public float DpiScale { get; set; } = 1.0f;
        public int DefaultWidth { get; set; } = 350;
        public int MinHeight { get; set; } = 75;
        public int MaxHeight { get; set; } = 250;
        public int Padding { get; set; } = 14;
        public int Spacing { get; set; } = 10;
        public int CornerRadius { get; set; } = 8;
        public TextOverflowMode TextOverflowMode { get; set; } = TextOverflowMode.Ellipsis;

        public int Scale(int value)
        {
            return (int)(value * DpiScale + 0.5f);
        }

        public float Scale(float value)
        {
            return value * DpiScale;
        }
    }

    /// <summary>
    /// Current visual rendering state of a notification toast during presentation and animation.
    /// </summary>
    public sealed class NotificationVisualState
    {
        public float Opacity { get; set; } = 1.0f;
        public float SlideOffsetRatio { get; set; } = 0.0f;
        public double RemainingDurationRatio { get; set; } = 1.0;
        public bool IsHovered { get; set; } = false;
        public NotificationHitArea HoveredArea { get; set; } = NotificationHitArea.None;
        public int HoveredActionIndex { get; set; } = -1;
    }
}
