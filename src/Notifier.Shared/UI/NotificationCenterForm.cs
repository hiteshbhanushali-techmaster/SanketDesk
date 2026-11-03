#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Notifier.History;
using Notifier.Templates;

namespace Notifier.UI
{
    /// <summary>
    /// Ready-to-use built-in WinForms Notification Center / Notification Feed window.
    /// Provides filtering by type/unread, newest-first chronological feed,
    /// mark-as-read actions, clearing history, and interactive action button triggers.
    /// Double-buffered and DPI-scaled.
    /// </summary>
    public sealed class NotificationCenterForm : Form
    {
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private readonly INotificationHistoryService _historyService;
        private readonly NotificationConfig _config;
        private readonly float _dpiScale;

        // UI State
        private int _selectedFilterIndex = 0; // 0=All, 1=Unread, 2=Error, 3=Warning, 4=Info, 5=Success
        private readonly string[] _filters = { "All", "Unread", "Errors", "Warnings", "Info", "Success" };
        private List<NotificationSnapshot> _filteredItems;
        private int _scrollOffset = 0;
        private int _totalContentHeight = 0;
        private int _hoveredItemIndex = -1;
        private bool _hoveredMarkAllRead = false;
        private bool _hoveredClearAll = false;
        private bool _hoveredClose = false;
        private int _hoveredFilterIndex = -1;

        public NotificationCenterForm(INotificationHistoryService historyService, NotificationConfig config)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            Screen screen = ToastPositionManager.ResolveTargetScreen(config.HostForm);
            _dpiScale = DpiHelper.GetDpiScale(screen);

            _filteredItems = new List<NotificationSnapshot>();

            // Form properties
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            int baseWidth = (int)(380 * _dpiScale);
            int baseHeight = (int)(520 * _dpiScale);
            Size = new Size(baseWidth, baseHeight);

            _historyService.NotificationAdded += OnNotificationAdded;
            _historyService.UnreadCountChanged += OnUnreadCountChanged;

            ReloadItems();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private void OnNotificationAdded(NotificationSnapshot snapshot)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke((Action)ReloadItems);
            }
        }

        private void OnUnreadCountChanged(int count)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke((Action)Invalidate);
            }
        }

        public void ReloadItems()
        {
            IReadOnlyList<NotificationSnapshot> all = _historyService.GetAll();
            _filteredItems.Clear();

            for (int i = 0; i < all.Count; i++)
            {
                NotificationSnapshot item = all[i];
                bool match = false;
                switch (_selectedFilterIndex)
                {
                    case 0: match = true; break; // All
                    case 1: match = !item.IsRead; break; // Unread
                    case 2: match = item.Type == NotificationType.Error; break;
                    case 3: match = item.Type == NotificationType.Warning; break;
                    case 4: match = item.Type == NotificationType.Info; break;
                    case 5: match = item.Type == NotificationType.Success; break;
                }

                if (match)
                {
                    _filteredItems.Add(item);
                }
            }

            _scrollOffset = 0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int pad = (int)(14 * _dpiScale);
            int headerH = (int)(50 * _dpiScale);
            int filterH = (int)(36 * _dpiScale);

            // 1. Background & Border
            Rectangle clientRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var bgBrush = new SolidBrush(Color.FromArgb(249, 250, 251))) // Slate 50
            {
                g.FillRectangle(bgBrush, clientRect);
            }
            using (var borderPen = new Pen(Color.FromArgb(209, 213, 219), 1.0f))
            {
                g.DrawRectangle(borderPen, clientRect);
            }

            // 2. Header Area
            Rectangle headerRect = new Rectangle(0, 0, Width, headerH);
            using (var headerBg = new SolidBrush(Color.White))
            {
                g.FillRectangle(headerBg, headerRect);
            }
            using (var headerBorder = new Pen(Color.FromArgb(229, 231, 235), 1.0f))
            {
                g.DrawLine(headerBorder, 0, headerH, Width, headerH);
            }

            // Header Title
            int unread = _historyService.GetUnreadCount();
            string titleText = unread > 0 ? $"Notifications ({unread})" : "Notifications";
            Font headerFont = GdiResourceCache.Instance.GetFont("Segoe UI", 11.5f * _dpiScale, FontStyle.Bold);
            using (var titleBrush = new SolidBrush(TemplateColors.TextPrimary))
            {
                g.DrawString(titleText, headerFont, titleBrush, pad, (headerH - (int)g.MeasureString(titleText, headerFont).Height) / 2);
            }

            // Header Action Buttons: "Mark all read", "Clear", "Close X"
            Font smallBtnFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Regular);
            int rightX = Width - pad;

            // Close button 'X'
            int closeBtnSize = (int)(20 * _dpiScale);
            Rectangle closeRect = new Rectangle(rightX - closeBtnSize, (headerH - closeBtnSize) / 2, closeBtnSize, closeBtnSize);
            VectorIconRenderer.DrawCloseButton(g, closeRect, _hoveredClose);
            rightX = closeRect.Left - (int)(10 * _dpiScale);

            // Clear button
            string clearText = "Clear";
            int clearW = (int)g.MeasureString(clearText, smallBtnFont).Width + (int)(10 * _dpiScale);
            Rectangle clearRect = new Rectangle(rightX - clearW, (headerH - (int)(24 * _dpiScale)) / 2, clearW, (int)(24 * _dpiScale));
            DrawSmallHeaderButton(g, clearRect, clearText, smallBtnFont, _hoveredClearAll);
            rightX = clearRect.Left - (int)(6 * _dpiScale);

            // Mark all read button
            if (unread > 0)
            {
                string markReadText = "Mark all read";
                int markW = (int)g.MeasureString(markReadText, smallBtnFont).Width + (int)(12 * _dpiScale);
                Rectangle markRect = new Rectangle(rightX - markW, (headerH - (int)(24 * _dpiScale)) / 2, markW, (int)(24 * _dpiScale));
                DrawSmallHeaderButton(g, markRect, markReadText, smallBtnFont, _hoveredMarkAllRead);
            }

            // 3. Filter Tabs
            Rectangle filterRect = new Rectangle(0, headerH, Width, filterH);
            using (var filterBg = new SolidBrush(Color.White))
            {
                g.FillRectangle(filterBg, filterRect);
            }
            using (var filterBorder = new Pen(Color.FromArgb(229, 231, 235), 1.0f))
            {
                g.DrawLine(filterBorder, 0, headerH + filterH, Width, headerH + filterH);
            }

            int tabX = pad;
            Font tabFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Regular);
            Font tabFontSelected = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Bold);

            for (int i = 0; i < _filters.Length; i++)
            {
                bool isSelected = i == _selectedFilterIndex;
                bool isHovered = i == _hoveredFilterIndex;
                string tabName = _filters[i];
                Font f = isSelected ? tabFontSelected : tabFont;
                SizeF tSize = g.MeasureString(tabName, f);
                int tabW = (int)tSize.Width + (int)(14 * _dpiScale);
                Rectangle tabBounds = new Rectangle(tabX, headerH + (int)(4 * _dpiScale), tabW, filterH - (int)(8 * _dpiScale));

                if (isSelected)
                {
                    using var activeTabBg = new SolidBrush(Color.FromArgb(240, 244, 255));
                    using var activeTabBorder = new Pen(TemplateColors.InfoAccent, 1.0f);
                    g.FillRectangle(activeTabBg, tabBounds);
                    g.DrawRectangle(activeTabBorder, tabBounds);
                }
                else if (isHovered)
                {
                    using var hoverTabBg = new SolidBrush(Color.FromArgb(243, 244, 246));
                    g.FillRectangle(hoverTabBg, tabBounds);
                }

                Color tabColor = isSelected ? TemplateColors.InfoAccent : (isHovered ? TemplateColors.TextPrimary : TemplateColors.TextSecondary);
                using (var tBrush = new SolidBrush(tabColor))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(tabName, f, tBrush, tabBounds, sf);
                }

                tabX += tabW + (int)(4 * _dpiScale);
            }

            // 4. Notification Items List Area
            int listTop = headerH + filterH;
            int listHeight = Height - listTop;
            Rectangle listClip = new Rectangle(0, listTop, Width, listHeight);
            g.SetClip(listClip);

            int curY = listTop + pad - _scrollOffset;

            if (_filteredItems.Count == 0)
            {
                // Empty state
                Font emptyFont = GdiResourceCache.Instance.GetFont("Segoe UI", 10.0f * _dpiScale, FontStyle.Regular);
                using (var emptyBrush = new SolidBrush(TemplateColors.TextMuted))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("No notifications to display", emptyFont, emptyBrush, listClip, sf);
                }
            }
            else
            {
                Font titleFont = GdiResourceCache.Instance.GetFont("Segoe UI", 9.5f * _dpiScale, FontStyle.Bold);
                Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Regular);
                Font timeFont = GdiResourceCache.Instance.GetFont("Segoe UI", 7.5f * _dpiScale, FontStyle.Regular);

                int itemW = Width - (pad * 2);

                for (int i = 0; i < _filteredItems.Count; i++)
                {
                    NotificationSnapshot item = _filteredItems[i];
                    bool isItemHovered = i == _hoveredItemIndex;

                    // Calculate card height
                    int iconSize = (int)(24 * _dpiScale);
                    int textW = itemW - iconSize - (int)(32 * _dpiScale);
                    SizeF mSize = g.MeasureString(item.Message, msgFont, textW);
                    int cardH = (int)(12 * _dpiScale) + (int)(18 * _dpiScale) + (int)mSize.Height + (int)(18 * _dpiScale) + (int)(8 * _dpiScale);

                    Rectangle itemRect = new Rectangle(pad, curY, itemW, cardH);

                    // Draw item card
                    using (var cardBg = new SolidBrush(isItemHovered ? Color.FromArgb(243, 244, 246) : Color.White))
                    {
                        g.FillRectangle(cardBg, itemRect);
                    }
                    using (var cardBorder = new Pen(item.IsRead ? Color.FromArgb(229, 231, 235) : TemplateColors.GetAccentColor(item.Type), item.IsRead ? 1.0f : 1.5f))
                    {
                        g.DrawRectangle(cardBorder, itemRect);
                    }

                    // Status icon
                    Rectangle iconRect = new Rectangle(itemRect.Left + (int)(10 * _dpiScale), itemRect.Top + (int)(12 * _dpiScale), iconSize, iconSize);
                    VectorIconRenderer.DrawNotificationTypeIcon(g, iconRect, item.Type);

                    // Unread blue dot indicator
                    if (!item.IsRead)
                    {
                        int dotR = (int)(3 * _dpiScale);
                        using var dotBrush = new SolidBrush(TemplateColors.InfoAccent);
                        g.FillEllipse(dotBrush, itemRect.Right - (int)(14 * _dpiScale), itemRect.Top + (int)(12 * _dpiScale), dotR * 2, dotR * 2);
                    }

                    // Title
                    int textX = iconRect.Right + (int)(10 * _dpiScale);
                    using (var tBrush = new SolidBrush(TemplateColors.TextPrimary))
                    {
                        string displayTitle = item.OccurrenceCount > 1 ? $"{item.Title} ({item.OccurrenceCount})" : item.Title;
                        g.DrawString(displayTitle, titleFont, tBrush, textX, itemRect.Top + (int)(10 * _dpiScale));
                    }

                    // Message
                    using (var mBrush = new SolidBrush(TemplateColors.TextSecondary))
                    {
                        RectangleF msgRect = new RectangleF(textX, itemRect.Top + (int)(28 * _dpiScale), textW, mSize.Height + 2);
                        g.DrawString(item.Message, msgFont, mBrush, msgRect);
                    }

                    // Timestamp
                    using (var timeBrush = new SolidBrush(TemplateColors.TextMuted))
                    {
                        string timeAgo = FormatTimeAgo(item.TimestampUtc);
                        g.DrawString(timeAgo, timeFont, timeBrush, textX, itemRect.Bottom - (int)(18 * _dpiScale));
                    }

                    curY += cardH + (int)(8 * _dpiScale);
                }
            }

            _totalContentHeight = curY + _scrollOffset - listTop;
            g.ResetClip();
        }

        private static void DrawSmallHeaderButton(Graphics g, Rectangle bounds, string text, Font font, bool isHovered)
        {
            if (isHovered)
            {
                using var hoverBg = new SolidBrush(Color.FromArgb(243, 244, 246));
                g.FillRectangle(hoverBg, bounds);
            }
            using var borderPen = new Pen(Color.FromArgb(209, 213, 219), 1.0f);
            g.DrawRectangle(borderPen, bounds);

            using var textBrush = new SolidBrush(isHovered ? TemplateColors.TextPrimary : TemplateColors.TextSecondary);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, textBrush, bounds, sf);
        }

        private static string FormatTimeAgo(DateTime timestampUtc)
        {
            TimeSpan diff = DateTime.UtcNow.Subtract(timestampUtc);
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return timestampUtc.ToLocalTime().ToString("MMM dd, h:mm tt");
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int maxScroll = Math.Max(0, _totalContentHeight - (Height - (int)(86 * _dpiScale)));
            _scrollOffset = Math.Max(0, Math.Min(maxScroll, _scrollOffset - (e.Delta / 2)));
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pt = e.Location;
            int pad = (int)(14 * _dpiScale);
            int headerH = (int)(50 * _dpiScale);
            int filterH = (int)(36 * _dpiScale);

            bool needRepaint = false;

            // Header buttons hit test
            int closeBtnSize = (int)(20 * _dpiScale);
            Rectangle closeRect = new Rectangle(Width - pad - closeBtnSize, (headerH - closeBtnSize) / 2, closeBtnSize, closeBtnSize);
            bool closeHover = closeRect.Contains(pt);
            if (_hoveredClose != closeHover) { _hoveredClose = closeHover; needRepaint = true; }

            // Filter tabs hit test
            int tabX = pad;
            int newFilterHover = -1;
            if (pt.Y >= headerH && pt.Y <= headerH + filterH)
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                Font tabFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Regular);

                for (int i = 0; i < _filters.Length; i++)
                {
                    int tabW = (int)g.MeasureString(_filters[i], tabFont).Width + (int)(14 * _dpiScale);
                    Rectangle tabBounds = new Rectangle(tabX, headerH, tabW, filterH);
                    if (tabBounds.Contains(pt))
                    {
                        newFilterHover = i;
                        break;
                    }
                    tabX += tabW + (int)(4 * _dpiScale);
                }
            }
            if (_hoveredFilterIndex != newFilterHover) { _hoveredFilterIndex = newFilterHover; needRepaint = true; }

            if (needRepaint) Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Point pt = e.Location;
            int pad = (int)(14 * _dpiScale);
            int headerH = (int)(50 * _dpiScale);
            int filterH = (int)(36 * _dpiScale);

            // Close button click
            int closeBtnSize = (int)(20 * _dpiScale);
            Rectangle closeRect = new Rectangle(Width - pad - closeBtnSize, (headerH - closeBtnSize) / 2, closeBtnSize, closeBtnSize);
            if (closeRect.Contains(pt))
            {
                Hide();
                return;
            }

            // Filter tab click
            int tabX = pad;
            if (pt.Y >= headerH && pt.Y <= headerH + filterH)
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                Font tabFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Regular);

                for (int i = 0; i < _filters.Length; i++)
                {
                    int tabW = (int)g.MeasureString(_filters[i], tabFont).Width + (int)(14 * _dpiScale);
                    Rectangle tabBounds = new Rectangle(tabX, headerH, tabW, filterH);
                    if (tabBounds.Contains(pt))
                    {
                        _selectedFilterIndex = i;
                        ReloadItems();
                        return;
                    }
                    tabX += tabW + (int)(4 * _dpiScale);
                }
            }

            // Clear all button click
            int clearW = (int)(50 * _dpiScale);
            Rectangle clearRect = new Rectangle(closeRect.Left - clearW - (int)(10 * _dpiScale), (headerH - (int)(24 * _dpiScale)) / 2, clearW, (int)(24 * _dpiScale));
            if (clearRect.Contains(pt))
            {
                _historyService.Clear();
                ReloadItems();
                return;
            }

            // Mark all read button click
            int markW = (int)(90 * _dpiScale);
            Rectangle markRect = new Rectangle(clearRect.Left - markW - (int)(6 * _dpiScale), (headerH - (int)(24 * _dpiScale)) / 2, markW, (int)(24 * _dpiScale));
            if (markRect.Contains(pt))
            {
                _historyService.MarkAllAsRead();
                ReloadItems();
                return;
            }

            // Clicking an item marks it as read
            int listTop = headerH + filterH;
            if (pt.Y > listTop && _filteredItems.Count > 0)
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                Font msgFont = GdiResourceCache.Instance.GetFont("Segoe UI", 8.5f * _dpiScale, FontStyle.Regular);
                int itemW = Width - (pad * 2);
                int curY = listTop + pad - _scrollOffset;

                for (int i = 0; i < _filteredItems.Count; i++)
                {
                    NotificationSnapshot item = _filteredItems[i];
                    int iconSize = (int)(24 * _dpiScale);
                    int textW = itemW - iconSize - (int)(32 * _dpiScale);
                    SizeF mSize = g.MeasureString(item.Message, msgFont, textW);
                    int cardH = (int)(12 * _dpiScale) + (int)(18 * _dpiScale) + (int)mSize.Height + (int)(18 * _dpiScale) + (int)(8 * _dpiScale);
                    Rectangle itemRect = new Rectangle(pad, curY, itemW, cardH);

                    if (itemRect.Contains(pt))
                    {
                        if (!item.IsRead)
                        {
                            _historyService.MarkAsRead(item.Id);
                            ReloadItems();
                        }
                        return;
                    }
                    curY += cardH + (int)(8 * _dpiScale);
                }
            }
        }
    }
}
