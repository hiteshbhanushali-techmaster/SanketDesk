#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Notifier.History;
using Notifier.Templates;

namespace Notifier.UI
{
    /// <summary>
    /// Lightweight draggable floating notification action icon with an unread badge (1-99, 99+).
    /// Strictly constrained within the host application window's boundary.
    /// Clicking opens or toggles the built-in Notification Center.
    /// Per-pixel alpha layered window (WS_EX_LAYERED) for 100% smooth, anti-aliased edges and DPI scaling.
    /// </summary>
    public sealed class FloatingIconForm : Form
    {
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_LAYERED = 0x00080000;

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int WM_CAPTURECHANGED = 0x0215;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
        private const int HTCLIENT = 1;

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const uint ULW_ALPHA = 0x00000002;

        private readonly Form _hostForm;
        private readonly NotificationConfig _config;
        private readonly INotificationHistoryService _historyService;
        private readonly Action _toggleNotificationCenter;
        private readonly float _dpiScale;

        private int _unreadCount;
        private bool _isDragging = false;
        private Point _dragCursorStart;
        private Point _dragFormStart;
        private bool _isHovered = false;

        /// <summary>
        /// Gets whether this form uses Win32 WS_EX_LAYERED for per-pixel alpha anti-aliasing.
        /// </summary>
        public bool IsLayeredWindow => (CreateParams.ExStyle & WS_EX_LAYERED) != 0;

        public FloatingIconForm(
            Form hostForm,
            NotificationConfig config,
            INotificationHistoryService historyService,
            Action toggleNotificationCenter)
        {
            _hostForm = hostForm ?? throw new ArgumentNullException(nameof(hostForm));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _toggleNotificationCenter = toggleNotificationCenter ?? throw new ArgumentNullException(nameof(toggleNotificationCenter));

            Screen screen = ToastPositionManager.ResolveTargetScreen(hostForm);
            _dpiScale = DpiHelper.GetDpiScale(screen);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            int baseSize = (int)(58 * _dpiScale);
            Size = new Size(baseSize, baseSize);

            _unreadCount = _historyService.GetUnreadCount();
            _historyService.UnreadCountChanged += OnUnreadCountChanged;

            // Hook host form events for position clamping and visibility
            _hostForm.LocationChanged += OnHostFormBoundsChanged;
            _hostForm.SizeChanged += OnHostFormBoundsChanged;
            _hostForm.Activated += OnHostFormActivated;
            _hostForm.Deactivate += OnHostFormDeactivate;
            _hostForm.FormClosing += OnHostFormClosing;

            // Initial placement: bottom-right corner of host form
            SnapToHostBottomRight();
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                // Prevent FloatingIconForm from activating, which prevents _hostForm from deactivating
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            if (m.Msg == WM_CAPTURECHANGED)
            {
                _isDragging = false;
            }
            if (m.Msg == WM_NCHITTEST)
            {
                if (_isDragging)
                {
                    m.Result = (IntPtr)HTCLIENT;
                    return;
                }

                Point screenPt = new Point(
                    unchecked((short)(m.LParam.ToInt64() & 0xFFFF)),
                    unchecked((short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                Point clientPt = PointToClient(screenPt);
                if (!IsInInteractiveArea(clientPt))
                {
                    m.Result = (IntPtr)HTTRANSPARENT;
                    return;
                }
                m.Result = (IntPtr)HTCLIENT;
                return;
            }
            base.WndProc(ref m);
        }

        private bool IsInInteractiveArea(Point pt)
        {
            int padding = (int)(4 * _dpiScale);
            int iconSize = (int)(46 * _dpiScale);
            Rectangle circleRect = new Rectangle(padding, Height - iconSize - padding, iconSize, iconSize);

            // Check circle boundary
            float cx = circleRect.Left + circleRect.Width / 2f;
            float cy = circleRect.Top + circleRect.Height / 2f;
            float r = circleRect.Width / 2f;
            float dx = pt.X - cx;
            float dy = pt.Y - cy;
            if ((dx * dx) + (dy * dy) <= (r * r))
            {
                return true;
            }

            // Check unread badge boundary if present
            if (_unreadCount > 0)
            {
                Rectangle badgeRect = CalculateBadgeRectangle(null);
                if (badgeRect.Contains(pt))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RenderLayeredWindow();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                RenderLayeredWindow();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RenderLayeredWindow();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Layered window handles background via 32-bit ARGB alpha channel in UpdateLayeredWindow
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Visual presentation is managed via UpdateLayeredWindow
        }

        private void RenderLayeredWindow()
        {
            if (!IsHandleCreated || IsDisposed) return;

            int width = Width;
            int height = Height;
            if (width <= 0 || height <= 0) return;

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                DrawContent(g);
            }

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);

                POINT ptDst = new POINT { x = Left, y = Top };
                SIZE size = new SIZE { cx = width, cy = height };
                POINT ptSrc = new POINT { x = 0, y = 0 };

                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(Handle, screenDc, ref ptDst, ref size, memDc, ref ptSrc, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);
                }
                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        private Rectangle CalculateBadgeRectangle(Graphics? g)
        {
            string badgeText = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
            Font badgeFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.0f * _dpiScale, FontStyle.Bold);

            SizeF textSize;
            if (g != null)
            {
                textSize = g.MeasureString(badgeText, badgeFont);
            }
            else
            {
                using var bmp = new Bitmap(1, 1);
                using var tempG = Graphics.FromImage(bmp);
                textSize = tempG.MeasureString(badgeText, badgeFont);
            }

            int badgeH = (int)(18 * _dpiScale);
            int badgeW = Math.Max(badgeH, (int)textSize.Width + (int)(8 * _dpiScale));
            int padding = (int)(4 * _dpiScale);
            return new Rectangle(Width - badgeW - padding, padding, badgeW, badgeH);
        }

        private void OnUnreadCountChanged(int count)
        {
            _unreadCount = count;
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke((Action)(() =>
                {
                    RenderLayeredWindow();
                }));
            }
        }

        private void OnHostFormBoundsChanged(object? sender, EventArgs e)
        {
            if (_hostForm.WindowState == FormWindowState.Minimized)
            {
                Hide();
                return;
            }

            if (!Visible && _hostForm.WindowState != FormWindowState.Minimized)
            {
                Show();
            }

            // Clamp current position within host boundaries
            ClampWithinHostBounds();
        }

        private void OnHostFormActivated(object? sender, EventArgs e)
        {
            if (!Visible && _hostForm.WindowState != FormWindowState.Minimized)
            {
                Show();
            }
        }

        private void OnHostFormDeactivate(object? sender, EventArgs e)
        {
            // Do not hide if user is actively dragging the icon
            if (_isDragging) return;

            if (!_config.KeepFloatingIconOnAppDeactivate)
            {
                Hide();
            }
        }

        private void OnHostFormClosing(object? sender, FormClosingEventArgs e)
        {
            Hide();
        }

        public void SnapToHostBottomRight()
        {
            if (!_hostForm.IsHandleCreated || _hostForm.IsDisposed) return;

            Point clientBottomRight = new Point(
                _hostForm.ClientSize.Width - Width - (int)(20 * _dpiScale),
                _hostForm.ClientSize.Height - Height - (int)(20 * _dpiScale));

            Point screenPt = _hostForm.PointToScreen(clientBottomRight);
            Location = CalculateClampedLocation(screenPt.X, screenPt.Y);
        }

        private Point CalculateClampedLocation(int targetX, int targetY)
        {
            if (!_hostForm.IsHandleCreated || _hostForm.IsDisposed)
                return new Point(targetX, targetY);

            Point hostScreenTopLeft = _hostForm.PointToScreen(Point.Empty);
            int minX = hostScreenTopLeft.X;
            int minY = hostScreenTopLeft.Y;
            int maxX = minX + _hostForm.ClientSize.Width - Width;
            int maxY = minY + _hostForm.ClientSize.Height - Height;

            if (maxX < minX) maxX = minX;
            if (maxY < minY) maxY = minY;

            int clampedX = Math.Max(minX, Math.Min(maxX, targetX));
            int clampedY = Math.Max(minY, Math.Min(maxY, targetY));

            return new Point(clampedX, clampedY);
        }

        /// <summary>
        /// Strictly constrains the floating icon inside the host application form's client boundary.
        /// Cannot escape or be dragged outside the host window.
        /// </summary>
        public void ClampWithinHostBounds()
        {
            Location = CalculateClampedLocation(Left, Top);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && IsInInteractiveArea(e.Location))
            {
                _isDragging = true;
                Capture = true;
                _dragCursorStart = Cursor.Position;
                _dragFormStart = Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                Point curCursor = Cursor.Position;
                int deltaX = curCursor.X - _dragCursorStart.X;
                int deltaY = curCursor.Y - _dragCursorStart.Y;

                Location = CalculateClampedLocation(_dragFormStart.X + deltaX, _dragFormStart.Y + deltaY);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                Capture = false;
                if (_isDragging)
                {
                    _isDragging = false;
                    Point curCursor = Cursor.Position;
                    int diffX = Math.Abs(curCursor.X - _dragCursorStart.X);
                    int diffY = Math.Abs(curCursor.Y - _dragCursorStart.Y);

                    // If mouse moved less than 5 pixels, treat as click
                    if (diffX < 5 && diffY < 5)
                    {
                        _toggleNotificationCenter();
                    }
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Cursor = Cursors.Hand;
            RenderLayeredWindow();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Cursor = Cursors.Default;
            RenderLayeredWindow();
        }

        private void DrawContent(Graphics g)
        {
            int padding = (int)(4 * _dpiScale);
            int iconSize = (int)(46 * _dpiScale);
            Rectangle circleRect = new Rectangle(padding, Height - iconSize - padding, iconSize, iconSize);

            // 1. Soft ambient drop shadow around the floating circular button
            using (var shadowBrush1 = new SolidBrush(Color.FromArgb(28, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush1, circleRect.X, circleRect.Y + (int)(2 * _dpiScale), circleRect.Width, circleRect.Height);
            }
            using (var shadowBrush2 = new SolidBrush(Color.FromArgb(16, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush2, circleRect.X - (int)(1 * _dpiScale), circleRect.Y + (int)(1 * _dpiScale), circleRect.Width + (int)(2 * _dpiScale), circleRect.Height + (int)(2 * _dpiScale));
            }

            // 2. Main Circular Emerald Green Body
            Color mainColor = _isHovered ? Color.FromArgb(0, 150, 136) : Color.FromArgb(37, 211, 102); // Modern Emerald Green
            using (var circleBrush = new SolidBrush(mainColor))
            {
                g.FillEllipse(circleBrush, circleRect);
            }

            // 3. Subtle Inset White Highlight Ring (Inset alignment prevents outer pixel bleeding or clipping)
            using (var ringPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f * _dpiScale))
            {
                ringPen.Alignment = PenAlignment.Inset;
                g.DrawEllipse(ringPen, circleRect);
            }

            // 4. Draw Bell Icon or Custom Image
            if (_config.FloatingIconImage != null)
            {
                int imgPad = (int)(10 * _dpiScale);
                Rectangle imgRect = new Rectangle(circleRect.Left + imgPad, circleRect.Top + imgPad, circleRect.Width - (imgPad * 2), circleRect.Height - (imgPad * 2));
                g.DrawImage(_config.FloatingIconImage, imgRect);
            }
            else
            {
                int bellPad = (int)(10 * _dpiScale);
                Rectangle bellRect = new Rectangle(circleRect.Left + bellPad, circleRect.Top + bellPad, circleRect.Width - (bellPad * 2), circleRect.Height - (bellPad * 2));
                VectorIconRenderer.DrawBellIcon(g, bellRect, Color.White);
            }

            // 5. Draw Unread Badge if unread > 0
            if (_unreadCount > 0)
            {
                string badgeText = _unreadCount > 99 ? "99+" : _unreadCount.ToString();
                Font badgeFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.0f * _dpiScale, FontStyle.Bold);

                Rectangle badgeRect = CalculateBadgeRectangle(g);

                // Subtle badge drop shadow
                using (var bShadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                {
                    Rectangle bShadowRect = new Rectangle(badgeRect.X, badgeRect.Y + (int)(1 * _dpiScale), badgeRect.Width, badgeRect.Height);
                    using var bShadowPath = CreateRoundedRectanglePath(bShadowRect, bShadowRect.Height / 2);
                    g.FillPath(bShadow, bShadowPath);
                }

                // Draw Red Badge Pill
                using (var badgeBrush = new SolidBrush(Color.FromArgb(239, 68, 68))) // Crimson Red
                {
                    using var path = CreateRoundedRectanglePath(badgeRect, badgeRect.Height / 2);
                    g.FillPath(badgeBrush, path);
                }

                // Inset white outline
                using (var outlinePen = new Pen(Color.White, 1.5f * _dpiScale))
                {
                    outlinePen.Alignment = PenAlignment.Inset;
                    using var path = CreateRoundedRectanglePath(badgeRect, badgeRect.Height / 2);
                    g.DrawPath(outlinePen, path);
                }

                // Badge text
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(badgeText, badgeFont, textBrush, badgeRect, sf);
                }
            }
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _historyService.UnreadCountChanged -= OnUnreadCountChanged;
                _hostForm.LocationChanged -= OnHostFormBoundsChanged;
                _hostForm.SizeChanged -= OnHostFormBoundsChanged;
                _hostForm.Activated -= OnHostFormActivated;
                _hostForm.Deactivate -= OnHostFormDeactivate;
                _hostForm.FormClosing -= OnHostFormClosing;
            }
            base.Dispose(disposing);
        }

        #region Win32 Interop
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst,
            ref POINT pptDst,
            ref SIZE psize,
            IntPtr hdcSrc,
            ref POINT pptSrc,
            uint crKey,
            [In] ref BLENDFUNCTION pblend,
            uint dwFlags);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }
        #endregion
    }
}
