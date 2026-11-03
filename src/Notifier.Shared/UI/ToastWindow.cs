#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Notifier.UI
{
    /// <summary>
    /// Double-buffered, non-activating, top-level WinForms window hosting a rendered notification toast.
    /// Does not steal focus from the active foreground window.
    /// Manages hit-testing, hover detection (for countdown pausing), and click dispatch.
    /// </summary>
    internal sealed class ToastWindow : Form
    {
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private readonly NotificationModel _model;
        private readonly INotificationTemplateRenderer _renderer;
        private readonly NotificationLayoutSettings _layout;
        private readonly NotificationVisualState _visualState;
        private readonly Action<ToastWindow, ToastDismissReason> _dismissCallback;

        public NotificationModel Model => _model;
        public NotificationVisualState VisualState => _visualState;
        public NotificationLayoutSettings LayoutSettings => _layout;

        public ToastWindow(
            NotificationModel model,
            INotificationTemplateRenderer renderer,
            NotificationLayoutSettings layout,
            Action<ToastWindow, ToastDismissReason> dismissCallback)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _dismissCallback = dismissCallback ?? throw new ArgumentNullException(nameof(dismissCallback));

            _visualState = new NotificationVisualState();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void UpdateDimensions(Graphics g)
        {
            Size measured = _renderer.MeasureSize(g, _model, _layout);
            Size = measured;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _renderer.Render(e.Graphics, ClientRectangle, _model, _visualState, _layout);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _visualState.IsHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _visualState.IsHovered = false;
            _visualState.HoveredArea = NotificationHitArea.None;
            _visualState.HoveredActionIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            NotificationHitTestResult hit = _renderer.HitTest(e.Location, ClientRectangle, _model, _layout);
            bool stateChanged = false;

            if (_visualState.HoveredArea != hit.HitArea || _visualState.HoveredActionIndex != hit.ActionIndex)
            {
                _visualState.HoveredArea = hit.HitArea;
                _visualState.HoveredActionIndex = hit.ActionIndex;
                stateChanged = true;
            }

            if (hit.HitArea == NotificationHitArea.CloseButton || hit.HitArea == NotificationHitArea.ActionButton)
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }

            if (stateChanged)
            {
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button != MouseButtons.Left) return;

            NotificationHitTestResult hit = _renderer.HitTest(e.Location, ClientRectangle, _model, _layout);

            if (hit.HitArea == NotificationHitArea.CloseButton)
            {
                _dismissCallback(this, ToastDismissReason.UserClose);
                return;
            }

            if (hit.HitArea == NotificationHitArea.ActionButton && hit.ActionIndex >= 0 && _model.Actions != null && hit.ActionIndex < _model.Actions.Count)
            {
                NotificationAction action = _model.Actions[hit.ActionIndex];
                try
                {
                    action.Callback?.Invoke(_model.Id);
                }
                catch { }

                _dismissCallback(this, ToastDismissReason.ActionClicked);
                return;
            }

            if (hit.HitArea == NotificationHitArea.Body)
            {
                try
                {
                    _model.OnClick?.Invoke(_model.Id);
                }
                catch { }

                _dismissCallback(this, ToastDismissReason.UserClick);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    _model.OnDoubleClick?.Invoke(_model.Id);
                }
                catch { }
            }
        }
    }
}
