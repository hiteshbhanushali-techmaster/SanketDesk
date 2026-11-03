using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notifier.Engine;

namespace Notifier.Tests.UnitTests
{
    [TestClass]
    public class PriorityTests
    {
        [TestMethod]
        public void SystemPriority_StrictlyOrders_Error_Warning_Info_Success()
        {
            var comparer = new StrictPriorityComparer(PriorityMode.SystemPriority);

            var success = CreateModel(1, NotificationType.Success);
            var info = CreateModel(2, NotificationType.Info);
            var warning = CreateModel(3, NotificationType.Warning);
            var error = CreateModel(4, NotificationType.Error);

            var list = new List<NotificationModel> { success, info, warning, error };
            list.Sort(comparer);

            Assert.AreEqual(NotificationType.Error, list[0].Type);
            Assert.AreEqual(NotificationType.Warning, list[1].Type);
            Assert.AreEqual(NotificationType.Info, list[2].Type);
            Assert.AreEqual(NotificationType.Success, list[3].Type);
        }

        [TestMethod]
        public void SystemPriority_Preserves_FIFO_WithinEqualSeverity()
        {
            var comparer = new StrictPriorityComparer(PriorityMode.SystemPriority);

            var warn1 = CreateModel(10, NotificationType.Warning);
            var warn2 = CreateModel(11, NotificationType.Warning);
            var warn3 = CreateModel(12, NotificationType.Warning);

            var list = new List<NotificationModel> { warn2, warn3, warn1 };
            list.Sort(comparer);

            Assert.AreEqual(10L, list[0].Id);
            Assert.AreEqual(11L, list[1].Id);
            Assert.AreEqual(12L, list[2].Id);
        }

        [TestMethod]
        public void EqualPriority_Preserves_StrictFIFO()
        {
            var comparer = new StrictPriorityComparer(PriorityMode.EqualPriority);

            var err = CreateModel(1, NotificationType.Error);
            var info = CreateModel(2, NotificationType.Info);
            var succ = CreateModel(3, NotificationType.Success);

            var list = new List<NotificationModel> { info, succ, err };
            list.Sort(comparer);

            Assert.AreEqual(1L, list[0].Id);
            Assert.AreEqual(2L, list[1].Id);
            Assert.AreEqual(3L, list[2].Id);
        }

        private static NotificationModel CreateModel(long id, NotificationType type)
        {
            return new NotificationModel(
                id,
                type,
                "Title " + id,
                "Message " + id,
                null,
                null,
                "ModernCard",
                NotificationPriority.SystemDefault,
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
        }
    }
}
