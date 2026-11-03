using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Notifier.Tests.StressTests
{
    [TestClass]
    public class DisposalRaceTests
    {
        [TestMethod]
        public void RapidDispose_WhileProducersAreActive_DoesNotDeadlock_OrCrash()
        {
            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.Queue,
                MaxPendingQueueCapacity = 10000,
                EnableHistory = false
            };

            Notifier.Initialize(config);

            var cts = new CancellationTokenSource();
            var producerTasks = new Task[8];

            // Start 8 continuous producer tasks hammering submissions
            for (int i = 0; i < producerTasks.Length; i++)
            {
                int taskId = i;
                producerTasks[i] = Task.Run(() =>
                {
                    long count = 0;
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            Notifier.ShowInfo("Title " + taskId, "Message " + count++);
                        }
                        catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException)
                        {
                            // Expected once Dispose() is called
                            break;
                        }
                    }
                });
            }

            // Let them run for 50ms then call Dispose() concurrently from worker thread
            Thread.Sleep(50);

            var disposeTask = Task.Run(() =>
            {
                Notifier.Dispose();
            });

            // Dispose must complete within 5 seconds without deadlocking even under heavy thread starvation
            bool completed = disposeTask.Wait(5000);
            Assert.IsTrue(completed, "Notifier.Dispose() deadlocked during concurrent producer burst!");

            cts.Cancel();
            Task.WaitAll(producerTasks, 1000);

            Assert.IsFalse(Notifier.IsInitialized);
        }
    }
}
