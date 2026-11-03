#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Notifier.Engine
{
    /// <summary>
    /// Enforces admission control, burst strategies, and grouping BEFORE items enter internal pending queues.
    /// Guarantees bounded-memory invariants under 10,000+ req/sec burst traffic.
    /// </summary>
    internal sealed class BoundedAdmissionController
    {
        private readonly NotificationConfig _config;
        private readonly BoundedPendingQueue _queue;
        private readonly ConcurrentDictionary<string, NotificationModel> _pendingKeyMap;
        private int _isDisposed;

        public BoundedPendingQueue Queue => _queue;

        public BoundedAdmissionController(NotificationConfig config, BoundedPendingQueue queue)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _pendingKeyMap = new ConcurrentDictionary<string, NotificationModel>(StringComparer.Ordinal);
            _isDisposed = 0;
        }

        /// <summary>
        /// Attempts to admit a notification request.
        /// Returns the notification ID if admitted or coalesced; -1 if dropped or rejected.
        /// </summary>
        public long Admit(NotificationModel model)
        {
            if (Volatile.Read(ref _isDisposed) == 1)
            {
                return -1;
            }

            string groupKey = ResolveGroupingKey(model);

            switch (_config.BurstStrategy)
            {
                case BurstStrategy.CoalesceByKey:
                    if (!string.IsNullOrEmpty(groupKey))
                    {
                        // Check if a pending notification with this key already exists
                        if (_pendingKeyMap.TryGetValue(groupKey, out NotificationModel? existingPending))
                        {
                            // In-place atomic coalesce: update count and remaining duration, 0 new allocations
                            existingPending.IncrementOccurrence();
                            existingPending.ScheduledAtUtc = model.ScheduledAtUtc;
                            return existingPending.Id;
                        }

                        // Try to register as the active pending item for this key
                        if (_pendingKeyMap.TryAdd(groupKey, model))
                        {
                            if (_queue.TryEnqueue(model))
                            {
                                return model.Id;
                            }
                            else
                            {
                                // Queue is full, remove from key map and drop
                                _pendingKeyMap.TryRemove(groupKey, out _);
                                return -1;
                            }
                        }
                        else
                        {
                            // Another thread registered the key concurrently, update that entry
                            if (_pendingKeyMap.TryGetValue(groupKey, out existingPending))
                            {
                                existingPending.IncrementOccurrence();
                                existingPending.ScheduledAtUtc = model.ScheduledAtUtc;
                                return existingPending.Id;
                            }
                        }
                    }
                    // Fallback to normal bounded enqueue if key is empty
                    return _queue.TryEnqueue(model) ? model.Id : -1;

                case BurstStrategy.Drop:
                    // Strict door check: drop immediately if queue is full, zero allocations
                    if (_queue.Count >= _queue.MaxCapacity)
                    {
                        return -1;
                    }
                    return _queue.TryEnqueue(model) ? model.Id : -1;

                case BurstStrategy.Coalesce:
                    // If queue is near capacity, coalesce into a single active summary notification
                    if (_queue.Count >= _queue.MaxCapacity * 0.8)
                    {
                        const string globalSummaryKey = "__GlobalCoalesceSummary__";
                        if (_pendingKeyMap.TryGetValue(globalSummaryKey, out NotificationModel? summary))
                        {
                            summary.IncrementOccurrence();
                            return summary.Id;
                        }

                        var summaryModel = new NotificationModel(
                            model.Id,
                            NotificationType.Info,
                            "Multiple Notifications",
                            "High volume activity detected.",
                            null,
                            null,
                            _config.DefaultTemplate,
                            NotificationPriority.Info,
                            globalSummaryKey,
                            DateTime.UtcNow,
                            _config.DefaultDisplayDuration,
                            _config.Target,
                            _config.Position,
                            _config.AnimationMode,
                            _config.TextOverflowMode,
                            null,
                            null,
                            null,
                            null);

                        if (_pendingKeyMap.TryAdd(globalSummaryKey, summaryModel))
                        {
                            _queue.TryEnqueue(summaryModel);
                        }
                        return summaryModel.Id;
                    }
                    return _queue.TryEnqueue(model) ? model.Id : -1;

                case BurstStrategy.Queue:
                default:
                    return _queue.TryEnqueue(model) ? model.Id : -1;
            }
        }

        /// <summary>
        /// Called by the scheduler when a notification is dispatched out of pending state,
        /// allowing subsequent notifications with the same key to be registered.
        /// </summary>
        public void ReleasePendingKey(string groupKey)
        {
            if (!string.IsNullOrEmpty(groupKey))
            {
                _pendingKeyMap.TryRemove(groupKey, out _);
            }
        }

        public void Clear()
        {
            _pendingKeyMap.Clear();
            _queue.Clear();
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _isDisposed, 1);
            Clear();
        }

        private string ResolveGroupingKey(NotificationModel model)
        {
            switch (_config.GroupingStrategy)
            {
                case NotificationGroupingStrategy.ByGroupKey:
                    return model.GroupKey;

                case NotificationGroupingStrategy.ByTitle:
                    return model.Title;

                case NotificationGroupingStrategy.ByKeyOrTitle:
                default:
                    return !string.IsNullOrEmpty(model.GroupKey) ? model.GroupKey : model.Title;
            }
        }
    }
}
