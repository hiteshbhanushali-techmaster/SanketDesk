using System.Drawing;

namespace SanketDesk
{
    /// <summary>
    /// Contract implemented by visual template renderers.
    /// Renderers are stateless drawing engines that measure and paint notification cards.
    /// </summary>
    public interface INotificationTemplateRenderer
    {
        /// <summary>
        /// Gets the unique template name (e.g. ModernCard, MinimalBar, SolidAccent, SplitPill, ActionPrompt, WindowsFluent).
        /// </summary>
        string TemplateName { get; }

        /// <summary>
        /// Calculates the required card size for the given notification model, layout constraints, and DPI scaling.
        /// </summary>
        Size MeasureSize(Graphics g, NotificationModel model, NotificationLayoutSettings layout);

        /// <summary>
        /// Paints the notification card onto the provided double-buffered graphics context.
        /// </summary>
        void Render(Graphics g, Rectangle bounds, NotificationModel model, NotificationVisualState state, NotificationLayoutSettings layout);

        /// <summary>
        /// Performs hit-testing to identify which element was clicked or hovered at the given relative mouse point.
        /// </summary>
        NotificationHitTestResult HitTest(Point location, Rectangle bounds, NotificationModel model, NotificationLayoutSettings layout);
    }
}
