using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanketDesk.History;
using SanketDesk.UI;

namespace SanketDesk.Tests.UITests
{
    [TestClass]
    public class GeometryAndLayoutTests
    {
        [TestMethod]
        public void LayoutSettings_ScalesDpi_Accurately()
        {
            var layout100 = new NotificationLayoutSettings { DpiScale = 1.0f };
            var layout150 = new NotificationLayoutSettings { DpiScale = 1.5f };
            var layout200 = new NotificationLayoutSettings { DpiScale = 2.0f };

            Assert.AreEqual(20, layout100.Scale(20));
            Assert.AreEqual(30, layout150.Scale(20));
            Assert.AreEqual(40, layout200.Scale(20));
        }

        [TestMethod]
        public void ToastPositionManager_Calculates_NonOverlapping_Stacking()
        {
            Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            var toastSize = new Size(350, 75);
            var previousSizes = new[] { toastSize, toastSize, toastSize };

            Rectangle r0 = ToastPositionManager.CalculateBounds(
                screen,
                NotificationPosition.BottomRight,
                toastSize,
                0,
                previousSizes,
                20, 20, 10, 1.0f);

            Rectangle r1 = ToastPositionManager.CalculateBounds(
                screen,
                NotificationPosition.BottomRight,
                toastSize,
                1,
                previousSizes,
                20, 20, 10, 1.0f);

            // r1 should stack directly above r0 separated by spacing (10px)
            Assert.AreEqual(r0.Top - 10, r1.Bottom);
            Assert.AreEqual(r0.Left, r1.Left);
        }

        [TestMethod]
        public void FloatingIconForm_UsesLayeredWindow_WhenInitialized()
        {
            using var host = new Form();
            host.Size = new Size(800, 600);
            var config = new NotificationConfig { HostForm = host, EnableFloatingIcon = true };
            using var history = new NotificationHistoryService(config);

            using var floater = new FloatingIconForm(host, config, history, () => { });
            var _ = floater.Handle; // ensure handle is created

            Assert.IsTrue(floater.IsLayeredWindow, "Floater should use Win32 WS_EX_LAYERED for per-pixel alpha anti-aliasing.");
            Assert.IsTrue(floater.Width > 0, "Floater width must be positive.");
            Assert.IsTrue(floater.Height > 0, "Floater height must be positive.");
        }

        [TestMethod]
        public void FloatingIconForm_ClampsWithinHostBounds_Accurately()
        {
            using var host = new Form();
            host.StartPosition = FormStartPosition.Manual;
            host.Location = new Point(100, 100);
            host.ClientSize = new Size(500, 400);
            var config = new NotificationConfig { HostForm = host, EnableFloatingIcon = true };
            using var history = new NotificationHistoryService(config);

            using var floater = new FloatingIconForm(host, config, history, () => { });
            var _ = floater.Handle; // ensure handle is created
            var __ = host.Handle;

            floater.SnapToHostBottomRight();
            Point hostScreenTopLeft = host.PointToScreen(Point.Empty);

            // Assert floater is placed inside host client bounds
            Assert.IsTrue(floater.Left >= hostScreenTopLeft.X, "Floater left should be inside host");
            Assert.IsTrue(floater.Top >= hostScreenTopLeft.Y, "Floater top should be inside host");
            Assert.IsTrue(floater.Right <= hostScreenTopLeft.X + host.ClientSize.Width, "Floater right should be inside host");
            Assert.IsTrue(floater.Bottom <= hostScreenTopLeft.Y + host.ClientSize.Height, "Floater bottom should be inside host");
        }
    }
}
