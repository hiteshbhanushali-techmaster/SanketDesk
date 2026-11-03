#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace Notifier.Engine
{
    /// <summary>
    /// Core notification engine coordinator.
    /// Manages ID generation, bounded admission, scheduling, dispatching to UI, and safe disposal.
    /// Completely thread-safe across thousands of simultaneous producer threads.
    /// </summary>
    internal sealed class NotificationEngine : IDisposable
    {
        private readonly NotificationConfig _config;
        private readonly ISystemClock _clock;
        private readonly IDiagnosticLogger _logger;
        private readonly BoundedAdmissionController _admission;
        private readonly CoordinatedSchedulerPump _schedulerPump;
        private readonly Action<IReadOnlyList<NotificationModel>>? _uiBatchDispatcher;
        private readonly Action<NotificationModel>? _windowsNotificationDispatcher;
        private readonly Action<NotificationSnapshot>? _historyRecorder;

        private long _idCounter;
        private int _isDisposed;

        public bool IsDisposed => Volatile.Read(ref _isDisposed) == 1;

        public NotificationEngine(
            NotificationConfig config,
            ISystemClock clock,
            IDiagnosticLogger logger,
            Action<IReadOnlyList<NotificationModel>>? uiBatchDispatcher,
            Action<NotificationModel>? windowsNotificationDispatcher,
            Action<NotificationSnapshot>? historyRecorder)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiBatchDispatcher = uiBatchDispatcher;
            _windowsNotificationDispatcher = windowsNotificationDispatcher;
            _historyRecorder = historyRecorder;

            var queue = new BoundedPendingQueue(config.MaxPendingQueueCapacity);
            _admission = new BoundedAdmissionController(config, queue);
            _schedulerPump = new CoordinatedSchedulerPump(config, _admission, clock, OnReadyBatchDispatched);
            _idCounter = 0;
            _isDisposed = 0;
        }

        /// <summary>
        /// Allocates a new monotonically increasing 64-bit notification ID.
        /// </summary>
        public long NextNotificationId()
        {
            return Interlocked.Increment(ref _idCounter);
        }

        /// <summary>
        /// Submits a notification model into the bounded admission controller.
        /// Thread-safe and allocation-conscious under 10,000+ req/sec bursts.
        /// </summary>
        public long Submit(NotificationModel model)
        {
            if (Volatile.Read(ref _isDisposed) == 1)
            {
                throw new ObjectDisposedException(nameof(NotificationEngine), "Notifier has been disposed.");
            }

            return _admission.Admit(model);
        }

        /// <summary>
        /// Callback invoked by the single coordinated scheduler pump when a batch of notifications is ready.
        /// </summary>
        private void OnReadyBatchDispatched(IReadOnlyList<NotificationModel> readyBatch)
        {
            if (Volatile.Read(ref _isDisposed) == 1 || readyBatch.Count == 0)
                return;

            // 1. Record into history
            if (_config.EnableHistory && _historyRecorder != null)
            {
                for (int i = 0; i < readyBatch.Count; i++)
                {
                    try
                    {
                        _historyRecorder(readyBatch[i].ToSnapshot());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error recording notification in history", ex);
                    }
                }
            }

            // 2. Dispatch to Windows Native Notifications if requested
            if (_windowsNotificationDispatcher != null)
            {
                for (int i = 0; i < readyBatch.Count; i++)
                {
                    NotificationModel item = readyBatch[i];
                    if (item.Target == NotificationTarget.Windows || item.Target == NotificationTarget.Both)
                    {
                        try
                        {
                            _windowsNotificationDispatcher(item);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error dispatching Windows native notification", ex);
                        }
                    }
                }
            }

            // 3. Dispatch to InApp WinForms Toast UI if requested
            if (_uiBatchDispatcher != null)
            {
                List<NotificationModel>? inAppList = null;
                for (int i = 0; i < readyBatch.Count; i++)
                {
                    NotificationModel item = readyBatch[i];
                    if (item.Target == NotificationTarget.InApp || item.Target == NotificationTarget.Both)
                    {
                        inAppList ??= new List<NotificationModel>(readyBatch.Count);
                        inAppList.Add(item);
                    }
                }

                if (inAppList != null && inAppList.Count > 0)
                {
                    try
                    {
                        _uiBatchDispatcher(inAppList);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error dispatching in-app notification batch", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Thread-safe, non-blocking, deadlock-free disposal.
        /// Can be called from any thread (worker or UI).
        /// Immediately stops scheduler, discards pending notifications, and cleans up engine resources.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            // 1. Stop scheduler pump timer immediately
            try
            {
                _schedulerPump.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error disposing scheduler pump", ex);
            }

            // 2. Immediately discard all pending queues and admission tables
            try
            {
                _admission.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error disposing admission controller", ex);
            }
        }
    }
}
