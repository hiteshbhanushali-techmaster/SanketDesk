#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Notifier.UI
{
    /// <summary>
    /// Coordinates active visible toast notifications on the UI thread.
    /// Bounds visible toast windows to MaxVisibleNotifications (e.g. 5 max).
    /// Uses a single centralized WinForms timer for countdown progress and animations that stops
    /// completely when no toasts are visible (0% idle CPU).
    /// Pauses countdown when mouse is hovering; resumes remaining duration on mouse leave.
    /// </summary>
    internal sealed class ActiveToastManager : IDisposable
    {
        private readonly NotificationConfig _config;
        private readonly IUiDispatcher _dispatcher;
        private readonly Func<string, INotificationTemplateRenderer> _templateResolver;
        private readonly List<ToastWindow> _activeToasts;
        private readonly Queue<NotificationModel> _pendingDisplayQueue;
        private readonly System.Windows.Forms.Timer _animationTimer;
        private readonly NotificationLayoutSettings _layoutSettings;
        private long _lastTickTimestamp;
        private int _isDisposed;

        public ActiveToastManager(
            NotificationConfig config,
            IUiDispatcher dispatcher,
            Func<string, INotificationTemplateRenderer> templateResolver)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _templateResolver = templateResolver ?? throw new ArgumentNullException(nameof(templateResolver));

            _activeToasts = new List<ToastWindow>();
            _pendingDisplayQueue = new Queue<NotificationModel>();
            _layoutSettings = new NotificationLayoutSettings
            {
                TextOverflowMode = config.TextOverflowMode
            };

            _animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 25 // ~40 FPS animation and progress clock
            };
            _animationTimer.Tick += OnAnimationTick;

            _lastTickTimestamp = DateTime.UtcNow.Ticks;
            _isDisposed = 0;
        }

        /// <summary>
        /// Enqueues a batch of ready notifications for display on the UI thread.
        /// </summary>
        public void EnqueueBatchForDisplay(IReadOnlyList<NotificationModel> batch)
        {
            if (batch == null || batch.Count == 0) return;

            _dispatcher.Post(() =>
            {
                if (System.Threading.Volatile.Read(ref _isDisposed) == 1)
                    return;

                for (int i = 0; i < batch.Count; i++)
                {
                    NotificationModel model = batch[i];

                    // Check if already reached max visible capacity
                    if (_activeToasts.Count < _config.MaxVisibleNotifications)
                    {
                        ShowToastWindow(model);
                    }
                    else
                    {
                        _pendingDisplayQueue.Enqueue(model);
                    }
                }

                RestackActiveToasts();
                EnsureTimerState();
            });
        }

        private void ShowToastWindow(NotificationModel model)
        {
            string templateName = !string.IsNullOrEmpty(model.Template) ? model.Template : _config.DefaultTemplate;
            INotificationTemplateRenderer renderer = _templateResolver(templateName);

            Screen screen = ToastPositionManager.ResolveTargetScreen(_config.HostForm);
            _layoutSettings.DpiScale = DpiHelper.GetDpiScale(screen);

            var toast = new ToastWindow(model, renderer, _layoutSettings, OnToastDismissRequested);

            using (Graphics g = toast.CreateGraphics())
            {
                toast.UpdateDimensions(g);
            }

            if (_config.AnimationMode == AnimationMode.Fade || _config.AnimationMode == AnimationMode.FadeAndSlide)
            {
                toast.Opacity = 0.0;
                toast.VisualState.Opacity = 0.0f;
            }
            else
            {
                toast.Opacity = 1.0;
                toast.VisualState.Opacity = 1.0f;
            }

            _activeToasts.Add(toast);
            toast.Show();
        }

        private void OnToastDismissRequested(ToastWindow toast, ToastDismissReason reason)
        {
            toast.Model.DismissReason = reason;

            try
            {
                toast.Model.OnClose?.Invoke(toast.Model.Id);
            }
            catch { }

            _activeToasts.Remove(toast);

            try
            {
                toast.Close();
                toast.Dispose();
            }
            catch { }

            // Promote next pending notification if available
            if (_pendingDisplayQueue.Count > 0 && _activeToasts.Count < _config.MaxVisibleNotifications)
            {
                NotificationModel next = _pendingDisplayQueue.Dequeue();
                ShowToastWindow(next);
            }

            RestackActiveToasts();
            EnsureTimerState();
        }

        private void RestackActiveToasts()
        {
            if (_activeToasts.Count == 0) return;

            Screen screen = ToastPositionManager.ResolveTargetScreen(_config.HostForm);
            float dpiScale = DpiHelper.GetDpiScale(screen);

            var sizes = new List<Size>(_activeToasts.Count);
            for (int i = 0; i < _activeToasts.Count; i++)
            {
                sizes.Add(_activeToasts[i].Size);
            }

            for (int i = 0; i < _activeToasts.Count; i++)
            {
                ToastWindow toast = _activeToasts[i];
                NotificationPosition pos = toast.Model.Position;

                Rectangle targetBounds = ToastPositionManager.CalculateBounds(
                    screen,
                    pos,
                    toast.Size,
                    i,
                    sizes,
                    _config.HorizontalOffset,
                    _config.VerticalOffset,
                    _config.Spacing,
                    dpiScale);

                toast.Location = targetBounds.Location;
            }
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (_activeToasts.Count == 0)
            {
                EnsureTimerState();
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;
            double elapsedMs = (nowTicks - _lastTickTimestamp) / (double)TimeSpan.TicksPerMillisecond;
            _lastTickTimestamp = nowTicks;

            if (elapsedMs < 1.0) elapsedMs = 1.0;
            if (elapsedMs > 500.0) elapsedMs = 25.0; // clamp clock jump

            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                ToastWindow toast = _activeToasts[i];

                // 1. Hover countdown pause check
                if (!toast.VisualState.IsHovered)
                {
                    toast.Model.RemainingDuration = toast.Model.RemainingDuration.Subtract(TimeSpan.FromMilliseconds(elapsedMs));
                    double ratio = toast.Model.RemainingDuration.TotalMilliseconds / toast.Model.DisplayDuration.TotalMilliseconds;
                    toast.VisualState.RemainingDurationRatio = Math.Max(0.0, Math.Min(1.0, ratio));

                    if (toast.Model.RemainingDuration <= TimeSpan.Zero)
                    {
                        OnToastDismissRequested(toast, ToastDismissReason.Timeout);
                        continue;
                    }
                }

                // 2. Animation transitions
                if (_config.AnimationMode == AnimationMode.Fade || _config.AnimationMode == AnimationMode.FadeAndSlide)
                {
                    if (toast.VisualState.Opacity < 1.0f)
                    {
                        toast.VisualState.Opacity = Math.Min(1.0f, toast.VisualState.Opacity + (float)(elapsedMs / 200.0));
                        toast.Opacity = toast.VisualState.Opacity;
                    }
                }

                toast.Invalidate();
            }
        }

        private void EnsureTimerState()
        {
            if (_activeToasts.Count > 0)
            {
                if (!_animationTimer.Enabled)
                {
                    _lastTickTimestamp = DateTime.UtcNow.Ticks;
                    _animationTimer.Start();
                }
            }
            else
            {
                if (_animationTimer.Enabled)
                {
                    _animationTimer.Stop();
                }
            }
        }

        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            _dispatcher.Send(() =>
            {
                try
                {
                    _animationTimer.Stop();
                    _animationTimer.Dispose();
                }
                catch { }

                for (int i = _activeToasts.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _activeToasts[i].Close();
                        _activeToasts[i].Dispose();
                    }
                    catch { }
                }
                _activeToasts.Clear();
                _pendingDisplayQueue.Clear();
            });
        }
    }
}
