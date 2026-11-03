using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanketDesk.Engine;

namespace SanketDesk.Tests.StressTests
{
    [TestClass]
    public class BurstStressTests
    {
        [TestMethod]
        public void Burst_10000_Requests_PerSecond_UnderCoalesceByKey_Handles_Cleanly()
        {
            const int totalRequests = 10000;
            const int keyCount = 10;

            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.CoalesceByKey,
                GroupingStrategy = NotificationGroupingStrategy.ByGroupKey,
                MaxPendingQueueCapacity = 20000
            };

            var queue = new BoundedPendingQueue(20000);
            var admission = new BoundedAdmissionController(config, queue);

            var sw = Stopwatch.StartNew();

            // Run 20 concurrent tasks submitting 500 notifications each (total 10,000 requests)
            Parallel.For(0, totalRequests, new ParallelOptions { MaxDegreeOfParallelism = 20 }, i =>
            {
                string key = "Key_" + (i % keyCount);
                var model = new NotificationModel(
                    i,
                    NotificationType.Warning,
                    "Burst Warning " + (i % keyCount),
                    "Burst Message " + i,
                    null,
                    null,
                    "ModernCard",
                    NotificationPriority.Warning,
                    key,
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

                admission.Admit(model);
            });

            sw.Stop();

            // 10,000 requests must complete in under 1000ms (1 second)
            Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Expected 10,000 submissions under 1000ms, took {sw.ElapsedMilliseconds}ms");

            // Exactly 10 distinct pending keys admitted to queue
            Assert.AreEqual(keyCount, queue.Count);
        }

        [TestMethod]
        public void Burst_DropStrategy_Enforces_HardCapacity_AtDoor()
        {
            const int maxCapacity = 500;
            const int totalRequests = 10000;

            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.Drop,
                MaxPendingQueueCapacity = maxCapacity
            };

            var queue = new BoundedPendingQueue(maxCapacity);
            var admission = new BoundedAdmissionController(config, queue);

            int admitted = 0;
            int dropped = 0;

            Parallel.For(0, totalRequests, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
            {
                var model = new NotificationModel(
                    i,
                    NotificationType.Info,
                    "Title " + i,
                    "Message " + i,
                    null,
                    null,
                    "ModernCard",
                    NotificationPriority.Info,
                    string.Empty,
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

                long id = admission.Admit(model);
                if (id >= 0)
                {
                    System.Threading.Interlocked.Increment(ref admitted);
                }
                else
                {
                    System.Threading.Interlocked.Increment(ref dropped);
                }
            });

            Assert.AreEqual(totalRequests, admitted + dropped);
            Assert.AreEqual(maxCapacity, queue.Count);
            Assert.IsTrue(dropped > 9000, $"Expected >9000 dropped, got {dropped}");
        }
    }
}
