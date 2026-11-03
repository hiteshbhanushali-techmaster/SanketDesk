using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanketDesk.History;

namespace SanketDesk.Tests.UnitTests
{
    [TestClass]
    public class RetentionAndHistoryTests
    {
        [TestMethod]
        public void History_Enforces_HardLimit_MaxCount_FIFO_Eviction()
        {
            var config = new NotificationConfig
            {
                MaxHistoryCount = 5,
                MaxRetentionPeriod = TimeSpan.FromHours(1),
                StorageType = StorageType.Memory
            };

            using var service = new NotificationHistoryService(config);

            // Add 10 notifications
            for (int i = 1; i <= 10; i++)
            {
                service.Record(new NotificationSnapshot(
                    i,
                    NotificationType.Info,
                    "Title " + i,
                    "Message " + i,
                    DateTime.UtcNow.AddMinutes(i),
                    false,
                    1,
                    "Key"));
            }

            // Trigger prune
            var storage = new MemoryStorage();
            for (int i = 1; i <= 10; i++)
            {
                storage.Append(new NotificationSnapshot(
                    i,
                    NotificationType.Info,
                    "Title " + i,
                    "Message " + i,
                    DateTime.UtcNow.AddMinutes(i),
                    false,
                    1,
                    "Key"));
            }

            storage.Prune(5, TimeSpan.FromHours(24));

            IReadOnlyList<NotificationSnapshot> remaining = storage.GetAll();
            Assert.AreEqual(5, remaining.Count);

            // Newest are 10, 9, 8, 7, 6. Oldest 1, 2, 3, 4, 5 were pruned!
            Assert.AreEqual(10L, remaining[0].Id);
            Assert.AreEqual(6L, remaining[4].Id);
        }

        [TestMethod]
        public void ReadUnread_Status_DoesNot_Affect_Eviction_Priority()
        {
            var storage = new MemoryStorage();

            // Item 1 (oldest) is UNREAD
            storage.Append(new NotificationSnapshot(1, NotificationType.Info, "Old Unread", "Msg", DateTime.UtcNow.AddMinutes(1), false, 1, ""));
            // Item 2 is READ
            storage.Append(new NotificationSnapshot(2, NotificationType.Info, "Old Read", "Msg", DateTime.UtcNow.AddMinutes(2), true, 1, ""));
            // Item 3 (newest) is READ
            storage.Append(new NotificationSnapshot(3, NotificationType.Info, "New Read", "Msg", DateTime.UtcNow.AddMinutes(3), true, 1, ""));

            // Prune down to 1 item
            storage.Prune(1, TimeSpan.FromHours(24));

            IReadOnlyList<NotificationSnapshot> remaining = storage.GetAll();
            Assert.AreEqual(1, remaining.Count);
            // Oldest (item 1 and 2) must be evicted strictly by age, leaving only newest (item 3)
            // Even though item 1 was unread, it is NOT preserved preferentially!
            Assert.AreEqual(3L, remaining[0].Id);
        }

        [TestMethod]
        public void GetLatest_Returns_Newest_At_Top()
        {
            var storage = new MemoryStorage();
            storage.Append(new NotificationSnapshot(1, NotificationType.Info, "T1", "M1", DateTime.UtcNow, false, 1, ""));
            storage.Append(new NotificationSnapshot(2, NotificationType.Info, "T2", "M2", DateTime.UtcNow, false, 1, ""));
            storage.Append(new NotificationSnapshot(3, NotificationType.Info, "T3", "M3", DateTime.UtcNow, false, 1, ""));

            IReadOnlyList<NotificationSnapshot> latest = storage.GetLatest(2);
            Assert.AreEqual(2, latest.Count);
            Assert.AreEqual(3L, latest[0].Id); // Newest is at TOP
            Assert.AreEqual(2L, latest[1].Id);
        }
    }
}
