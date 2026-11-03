#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SanketDesk.UI
{
    /// <summary>
    /// Computes multi-monitor, taskbar-aware, DPI-scaled placement coordinates for active toast notifications.
    /// Supports stacking in 7 anchor positions without covering the Windows taskbar.
    /// </summary>
    internal static class ToastPositionManager
    {
        public static Screen ResolveTargetScreen(Form? hostForm)
        {
            if (hostForm != null && hostForm.IsHandleCreated && !hostForm.IsDisposed)
            {
                try
                {
                    return Screen.FromControl(hostForm);
                }
                catch { }
            }

            try
            {
                return Screen.FromPoint(Cursor.Position);
            }
            catch { }

            return Screen.PrimaryScreen ?? Screen.AllScreens[0];
        }

        public static Rectangle CalculateBounds(
            Screen screen,
            NotificationPosition position,
            Size toastSize,
            int stackIndex,
            IReadOnlyList<Size> previousSizes,
            int horizontalOffset,
            int verticalOffset,
            int spacing,
            float dpiScale)
        {
            Rectangle workingArea = screen.WorkingArea;

            int scaledHOffset = (int)(horizontalOffset * dpiScale + 0.5f);
            int scaledVOffset = (int)(verticalOffset * dpiScale + 0.5f);
            int scaledSpacing = (int)(spacing * dpiScale + 0.5f);

            int width = toastSize.Width;
            int height = toastSize.Height;

            // Compute accumulated vertical offset of prior stacked toasts
            int accumulatedHeight = 0;
            for (int i = 0; i < stackIndex && i < previousSizes.Count; i++)
            {
                accumulatedHeight += previousSizes[i].Height + scaledSpacing;
            }

            int x;
            int y;

            switch (position)
            {
                case NotificationPosition.TopLeft:
                    x = workingArea.Left + scaledHOffset;
                    y = workingArea.Top + scaledVOffset + accumulatedHeight;
                    break;

                case NotificationPosition.TopCenter:
                    x = workingArea.Left + (workingArea.Width - width) / 2;
                    y = workingArea.Top + scaledVOffset + accumulatedHeight;
                    break;

                case NotificationPosition.Top:
                case NotificationPosition.TopRight:
                    x = workingArea.Right - width - scaledHOffset;
                    y = workingArea.Top + scaledVOffset + accumulatedHeight;
                    break;

                case NotificationPosition.BottomLeft:
                    x = workingArea.Left + scaledHOffset;
                    y = workingArea.Bottom - scaledVOffset - accumulatedHeight - height;
                    break;

                case NotificationPosition.BottomCenter:
                    x = workingArea.Left + (workingArea.Width - width) / 2;
                    y = workingArea.Bottom - scaledVOffset - accumulatedHeight - height;
                    break;

                case NotificationPosition.Center:
                    x = workingArea.Left + (workingArea.Width - width) / 2;
                    int totalStackHeight = accumulatedHeight + height;
                    y = workingArea.Top + (workingArea.Height - totalStackHeight) / 2 + accumulatedHeight;
                    break;

                case NotificationPosition.Bottom:
                case NotificationPosition.BottomRight:
                default:
                    x = workingArea.Right - width - scaledHOffset;
                    y = workingArea.Bottom - scaledVOffset - accumulatedHeight - height;
                    break;
            }

            return new Rectangle(x, y, width, height);
        }
    }
}
