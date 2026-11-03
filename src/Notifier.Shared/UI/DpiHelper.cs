#nullable enable
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Notifier.UI
{
    /// <summary>
    /// Utility for detecting and calculating per-monitor DPI scale factors across 100%, 125%, 150%, and 200%+ displays.
    /// Compatible with both .NET Framework 4.6.1 and .NET 8.
    /// </summary>
    internal static class DpiHelper
    {
        private const int DefaultDpi = 96;

        public static float GetDpiScale(Screen screen)
        {
            try
            {
                IntPtr hMonitor = MonitorFromPoint(new POINT { x = screen.Bounds.Left + 1, y = screen.Bounds.Top + 1 }, 2 /* MONITOR_DEFAULTTONEAREST */);
                if (hMonitor != IntPtr.Zero)
                {
                    int hr = GetDpiForMonitor(hMonitor, 0 /* MDT_EFFECTIVE_DPI */, out uint dpiX, out uint _);
                    if (hr == 0 && dpiX > 0)
                    {
                        return dpiX / (float)DefaultDpi;
                    }
                }
            }
            catch { }

            // Fallback using Graphics DeviceContext
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    return g.DpiX / (float)DefaultDpi;
                }
            }
            catch
            {
                return 1.0f;
            }
        }

        public static float GetDpiScale(Control? control)
        {
            if (control != null && control.IsHandleCreated)
            {
                Screen screen = Screen.FromControl(control);
                return GetDpiScale(screen);
            }
            return GetDpiScale(Screen.PrimaryScreen ?? Screen.AllScreens[0]);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("shcore.dll", SetLastError = true)]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    }
}
