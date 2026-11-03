#nullable enable
using System.Collections.Generic;

namespace SanketDesk.Engine
{
    /// <summary>
    /// Implements strict priority sorting without aging or anti-starvation reordering.
    /// SystemPriority: Error (3) > Warning (2) > Info (1) > Success (0).
    /// Within equal severity, preserves strict FIFO by Notification ID.
    /// EqualPriority: Strict FIFO by Notification ID.
    /// </summary>
    internal sealed class StrictPriorityComparer : IComparer<NotificationModel>
    {
        private readonly PriorityMode _mode;

        public StrictPriorityComparer(PriorityMode mode)
        {
            _mode = mode;
        }

        public int Compare(NotificationModel? x, NotificationModel? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            if (_mode == PriorityMode.SystemPriority)
            {
                int pX = GetPriorityValue(x);
                int pY = GetPriorityValue(y);

                if (pX != pY)
                {
                    // Higher priority value first (e.g. Error=3 before Warning=2)
                    return pY.CompareTo(pX);
                }
            }

            // Within equal priority (or in EqualPriority mode), preserve strict FIFO by Id
            return x.Id.CompareTo(y.Id);
        }

        private static int GetPriorityValue(NotificationModel model)
        {
            if (model.Priority != NotificationPriority.SystemDefault)
            {
                return (int)model.Priority;
            }

            switch (model.Type)
            {
                case NotificationType.Error:
                    return 3;
                case NotificationType.Warning:
                    return 2;
                case NotificationType.Info:
                    return 1;
                case NotificationType.Success:
                    return 0;
                case NotificationType.Custom:
                default:
                    return 1; // Default Custom to Info level
            }
        }
    }
}
