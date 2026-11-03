#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Notifier.Engine
{
    /// <summary>
    /// Thread-safe memory-bounded pending queue with atomic capacity enforcement.
    /// Guarantees that total pending items never exceed MaxCapacity.
    /// </summary>
    internal sealed class BoundedPendingQueue
    {
        private readonly ConcurrentQueue<NotificationModel> _queue;
        private readonly int _maxCapacity;
        private int _count;

        public int MaxCapacity => _maxCapacity;
        public int Count => Volatile.Read(ref _count);

        public BoundedPendingQueue(int maxCapacity)
        {
            if (maxCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity must be positive.");

            _maxCapacity = maxCapacity;
            _queue = new ConcurrentQueue<NotificationModel>();
            _count = 0;
        }

        /// <summary>
        /// Attempts to enqueue an admitted notification.
        /// Returns true if enqueued; false if maximum capacity was reached.
        /// </summary>
        public bool TryEnqueue(NotificationModel model)
        {
            while (true)
            {
                int current = Volatile.Read(ref _count);
                if (current >= _maxCapacity)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _count, current + 1, current) == current)
                {
                    _queue.Enqueue(model);
                    return true;
                }
            }
        }

        /// <summary>
        /// Drains up to maxItems from the queue into the destination list.
        /// Returns the number of items drained.
        /// </summary>
        public int Drain(List<NotificationModel> destination, int maxItems)
        {
            int drained = 0;
            while (drained < maxItems && _queue.TryDequeue(out NotificationModel? item))
            {
                if (item != null)
                {
                    destination.Add(item);
                    drained++;
                    Interlocked.Decrement(ref _count);
                }
            }
            return drained;
        }

        /// <summary>
        /// Clears all items from the queue immediately.
        /// </summary>
        public void Clear()
        {
            while (_queue.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _count);
            }
            Interlocked.Exchange(ref _count, 0);
        }
    }
}
