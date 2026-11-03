#nullable enable
using System;
using System.Collections.Generic;

namespace Notifier.Engine
{
    /// <summary>
    /// Evaluates scheduling delays, fixed delays, debouncing, and batching windows.
    /// Operates synchronously during the single coordinated scheduler tick without creating per-notification timers.
    /// </summary>
    internal sealed class DelayAndDebounceManager
    {
        private readonly NotificationConfig _config;
        private readonly ISystemClock _clock;
        private readonly List<NotificationModel> _delayedItems;
        private readonly Dictionary<string, NotificationModel> _debouncedItems;
        private DateTime _batchWindowExpiryUtc;

        public DelayAndDebounceManager(NotificationConfig config, ISystemClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _delayedItems = new List<NotificationModel>();
            _debouncedItems = new Dictionary<string, NotificationModel>(StringComparer.Ordinal);
            _batchWindowExpiryUtc = DateTime.MinValue;
        }

        /// <summary>
        /// Evaluates a drained notification. If ready immediately, adds directly to readyDestination.
        /// Otherwise, retains in internal delay/debounce structures.
        /// </summary>
        public void ProcessIncoming(NotificationModel model, List<NotificationModel> readyDestination)
        {
            NotificationDelayMode delayMode = model.Tag is AdvancedNotificationOptions adv && adv.DelayMode.HasValue
                ? adv.DelayMode.Value
                : _config.DelayMode;

            TimeSpan delayInterval = model.Tag is AdvancedNotificationOptions advOpt && advOpt.Delay.HasValue
                ? advOpt.Delay.Value
                : _config.DelayInterval;

            if (delayMode == NotificationDelayMode.None || delayInterval <= TimeSpan.Zero)
            {
                readyDestination.Add(model);
                return;
            }

            DateTime now = _clock.UtcNow;

            switch (delayMode)
            {
                case NotificationDelayMode.Fixed:
                    model.ScheduledAtUtc = now.Add(delayInterval);
                    _delayedItems.Add(model);
                    break;

                case NotificationDelayMode.Debounce:
                    string key = !string.IsNullOrEmpty(model.GroupKey) ? model.GroupKey : model.Title;
                    if (_debouncedItems.TryGetValue(key, out NotificationModel? existing))
                    {
                        existing.IncrementOccurrence();
                        existing.ScheduledAtUtc = now.Add(delayInterval);
                    }
                    else
                    {
                        model.ScheduledAtUtc = now.Add(delayInterval);
                        _debouncedItems[key] = model;
                    }
                    break;

                case NotificationDelayMode.Batch:
                    if (_batchWindowExpiryUtc == DateTime.MinValue)
                    {
                        _batchWindowExpiryUtc = now.Add(delayInterval);
                    }
                    _delayedItems.Add(model);
                    break;

                default:
                    readyDestination.Add(model);
                    break;
            }
        }

        /// <summary>
        /// Checks all pending delayed, debounced, and batched items against current time and moves ready items into readyDestination.
        /// </summary>
        public void CollectReadyItems(List<NotificationModel> readyDestination)
        {
            DateTime now = _clock.UtcNow;

            // 1. Fixed delay items
            if (_delayedItems.Count > 0)
            {
                if (_config.DelayMode == NotificationDelayMode.Batch)
                {
                    if (now >= _batchWindowExpiryUtc)
                    {
                        readyDestination.AddRange(_delayedItems);
                        _delayedItems.Clear();
                        _batchWindowExpiryUtc = DateTime.MinValue;
                    }
                }
                else
                {
                    for (int i = _delayedItems.Count - 1; i >= 0; i--)
                    {
                        if (now >= _delayedItems[i].ScheduledAtUtc)
                        {
                            readyDestination.Add(_delayedItems[i]);
                            _delayedItems.RemoveAt(i);
                        }
                    }
                }
            }

            // 2. Debounced items
            if (_debouncedItems.Count > 0)
            {
                List<string>? readyKeys = null;
                foreach (var kvp in _debouncedItems)
                {
                    if (now >= kvp.Value.ScheduledAtUtc)
                    {
                        readyDestination.Add(kvp.Value);
                        readyKeys ??= new List<string>();
                        readyKeys.Add(kvp.Key);
                    }
                }

                if (readyKeys != null)
                {
                    for (int i = 0; i < readyKeys.Count; i++)
                    {
                        _debouncedItems.Remove(readyKeys[i]);
                    }
                }
            }
        }

        public void Clear()
        {
            _delayedItems.Clear();
            _debouncedItems.Clear();
            _batchWindowExpiryUtc = DateTime.MinValue;
        }
    }
}
