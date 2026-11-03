#nullable enable
using System;
using System.Threading;
using System.Windows.Forms;

namespace Notifier.UI
{
    /// <summary>
    /// Thread marshaling dispatcher for the WinForms UI thread.
    /// Ensures that UI controls are never touched directly from producer/background threads.
    /// </summary>
    internal sealed class UiDispatcher : IUiDispatcher
    {
        private readonly SynchronizationContext? _syncContext;
        private readonly Control? _invokerControl;
        private readonly int _uiThreadId;

        public UiDispatcher(Control? hostControl = null)
        {
            _syncContext = SynchronizationContext.Current;
            _invokerControl = hostControl;
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public bool CheckAccess()
        {
            return Thread.CurrentThread.ManagedThreadId == _uiThreadId;
        }

        public void Post(Action action)
        {
            if (action == null) return;

            if (CheckAccess())
            {
                action();
                return;
            }

            if (_syncContext != null)
            {
                _syncContext.Post(_ => action(), null);
                return;
            }

            if (_invokerControl != null && _invokerControl.IsHandleCreated && !_invokerControl.IsDisposed)
            {
                try
                {
                    _invokerControl.BeginInvoke(action);
                    return;
                }
                catch { }
            }

            // Fallback to thread pool if no UI message pump exists (e.g. headless/tests)
            ThreadPool.QueueUserWorkItem(_ => action());
        }

        public void Send(Action action)
        {
            if (action == null) return;

            if (CheckAccess())
            {
                action();
                return;
            }

            if (_syncContext != null)
            {
                _syncContext.Send(_ => action(), null);
                return;
            }

            if (_invokerControl != null && _invokerControl.IsHandleCreated && !_invokerControl.IsDisposed)
            {
                try
                {
                    _invokerControl.Invoke(action);
                    return;
                }
                catch { }
            }

            action();
        }
    }
}
