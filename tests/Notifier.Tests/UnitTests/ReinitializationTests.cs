using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Notifier.Tests.UnitTests
{
    [TestClass]
    public class ReinitializationTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            Notifier.Dispose();
        }

        [TestMethod]
        public void Reinitialize_Successfully_Reconfigures_Engine_Without_Error()
        {
            var config1 = new NotificationConfig
            {
                Position = NotificationPosition.BottomRight,
                DefaultTemplate = NotificationTemplateNames.ModernCard,
                EnableHistory = true,
                MaxHistoryCount = 100
            };

            Notifier.Initialize(config1);
            Assert.IsTrue(Notifier.IsInitialized);

            long id1 = Notifier.ShowSuccess("Init 1", "Message 1");
            Assert.IsTrue(id1 > 0);

            // Reinitialize with new configuration
            var config2 = new NotificationConfig
            {
                Position = NotificationPosition.TopLeft,
                DefaultTemplate = NotificationTemplateNames.WindowsFluent,
                EnableHistory = true,
                MaxHistoryCount = 200
            };

            Notifier.Reinitialize(config2);
            Assert.IsTrue(Notifier.IsInitialized);

            long id2 = Notifier.ShowInfo("Init 2", "Message 2");
            Assert.IsTrue(id2 > 0);

            Notifier.Dispose();
            Assert.IsFalse(Notifier.IsInitialized);
        }
    }
}
