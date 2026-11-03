#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace Notifier.Engine
{
    /// <summary>
    /// The single coordinated background scheduling pump.
    /// Drains admitted queue items, evaluates delays/debounces, applies strict priority sorting,
    /// and invokes the dispatch handler in batches.
    /// Uses exactly ONE timer for the entire notification engine.
    /// </summary>
    internal sealed class CoordinatedSchedulerPump : IDisposable
    {
        private readonly BoundedAdmissionController _admission;
        private readonly DelayAndDebounceManager _delayManager;
        private readonly StrictPriorityComparer _priorityComparer;
        private readonly Action<IReadOnlyList<NotificationModel>> _dispatchReadyBatch;
        private readonly Timer _timer;
        private readonly List<NotificationModel> _drainBuffer;
        private readonly List<NotificationModel> _readyBuffer;
        private int _isTicking;
        private int _isDisposed;

        public CoordinatedSchedulerPump(
            NotificationConfig config,
            BoundedAdmissionController admission,
            ISystemClock clock,
            Action<IReadOnlyList<NotificationModel>> dispatchReadyBatch)
        {
            _admission = admission ?? throw new ArgumentNullException(nameof(admission));
            _dispatchReadyBatch = dispatchReadyBatch ?? throw new ArgumentNullException(nameof(dispatchReadyBatch));
            _delayManager = new DelayAndDebounceManager(config, clock);
            _priorityComparer = new StrictPriorityComparer(config.PriorityMode);
            _drainBuffer = new List<NotificationModel>(256);
            _readyBuffer = new List<NotificationModel>(256);
            _isTicking = 0;
            _isDisposed = 0;

            // Single coordinated clock ticking every 25ms
            _timer = new Timer(OnTick, null, 25, 25);
        }

        private void OnTick(object? state)
        {
            if (Volatile.Read(ref _isDisposed) == 1)
                return;

            // Reentrancy guard
            if (Interlocked.CompareExchange(ref _isTicking, 1, 0) != 0)
                return;

            try
            {
                _drainBuffer.Clear();
                _readyBuffer.Clear();

                // 1. Drain up to 1000 items per tick from bounded queue
                int drained = _admission.Queue.Drain(_drainBuffer, 1000);

                // 2. Process drained items through delay/debounce manager
                for (int i = 0; i < drained; i++)
                {
                    _delayManager.ProcessIncoming(_drainBuffer[i], _readyBuffer);
                }

                // 3. Collect any fixed delay, debounce, or batch items that have matured
                _delayManager.CollectReadyItems(_readyBuffer);

                // 4. If any notifications are ready, sort strictly and dispatch
                if (_readyBuffer.Count > 0)
                {
                    if (_readyBuffer.Count > 1)
                    {
                        _readyBuffer.Sort(_priorityComparer);
                    }

                    // Release keys from admission map so subsequent bursts can be registered
                    for (int i = 0; i < _readyBuffer.Count; i++)
                    {
                        NotificationModel model = _readyBuffer[i];
                        string key = !string.IsNullOrEmpty(model.GroupKey) ? model.GroupKey : model.Title;
                        _admission.ReleasePendingKey(key);
                    }

                    // Dispatch batch to UI / History
                    _dispatchReadyBatch(_readyBuffer);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isTicking, 0);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            // Immediately stop timer
            try
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
            }
            catch { }

            _delayManager.Clear();
            _drainBuffer.Clear();
            _readyBuffer.Clear();
        }
    }
}
