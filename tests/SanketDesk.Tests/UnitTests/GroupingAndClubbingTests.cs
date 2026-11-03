using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanketDesk.Engine;

namespace SanketDesk.Tests.UnitTests
{
    [TestClass]
    public class GroupingAndClubbingTests
    {
        [TestMethod]
        public void CoalesceByKey_Aggregates_Occurrences_InPlace_Title_N()
        {
            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.CoalesceByKey,
                GroupingStrategy = NotificationGroupingStrategy.ByGroupKey,
                MaxPendingQueueCapacity = 1000
            };

            var queue = new BoundedPendingQueue(1000);
            var admission = new BoundedAdmissionController(config, queue);

            var first = CreateModel(1, "Order Rejected", "OrderRejected");
            long id1 = admission.Admit(first);
            Assert.AreEqual(1L, id1);
            Assert.AreEqual(1, first.OccurrenceCount);
            Assert.AreEqual("Order Rejected", first.GetFormattedTitle());

            // Second submission with same GroupKey
            var second = CreateModel(2, "Order Rejected", "OrderRejected");
            long id2 = admission.Admit(second);

            Assert.AreEqual(1L, id2); // Returns first notification's ID!
            Assert.AreEqual(2, first.OccurrenceCount); // In-place increment
            Assert.AreEqual("Order Rejected (2)", first.GetFormattedTitle());

            // Third submission
            var third = CreateModel(3, "Order Rejected", "OrderRejected");
            long id3 = admission.Admit(third);

            Assert.AreEqual(1L, id3);
            Assert.AreEqual(3, first.OccurrenceCount);
            Assert.AreEqual("Order Rejected (3)", first.GetFormattedTitle());

            // Queue should only have admitted the first item
            Assert.AreEqual(1, queue.Count);
        }

        [TestMethod]
        public void GroupingByTitle_Aggregates_WhenGroupKey_IsEmpty()
        {
            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.CoalesceByKey,
                GroupingStrategy = NotificationGroupingStrategy.ByTitle,
                MaxPendingQueueCapacity = 1000
            };

            var queue = new BoundedPendingQueue(1000);
            var admission = new BoundedAdmissionController(config, queue);

            var item1 = CreateModel(100, "Connection Error", string.Empty);
            var item2 = CreateModel(101, "Connection Error", string.Empty);

            long firstId = admission.Admit(item1);
            long secondId = admission.Admit(item2);

            Assert.AreEqual(firstId, secondId);
            Assert.AreEqual(2, item1.OccurrenceCount);
            Assert.AreEqual("Connection Error (2)", item1.GetFormattedTitle());
        }

        private static NotificationModel CreateModel(long id, string title, string groupKey)
        {
            return new NotificationModel(
                id,
                NotificationType.Error,
                title,
                "Detail message",
                null,
                null,
                "ModernCard",
                NotificationPriority.Error,
                groupKey,
                DateTime.UtcNow,
                TimeSpan.FromSeconds(5),
                NotificationTarget.InApp,
                NotificationPosition.BottomRight,
                AnimationMode.None,
                TextOverflowMode.Ellipsis,
                null,
                null,
                null,
                null);
        }
    }
}
