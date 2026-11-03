using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SanketDesk.Engine;
using SanketDesk.History;

namespace SanketDesk.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("    NOTIFIER BENCHMARK SUITE - PERFORMANCE & GC ANALYSIS       ");
            Console.WriteLine("================================================================");
            Console.WriteLine($"Runtime: .NET {Environment.Version}");
            Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine($"64-Bit Process: {Environment.Is64BitProcess}");
            Console.WriteLine();

            // Warm up
            WarmUp();

            // 1. 10,000 Burst Multi-Threaded Throughput
            Run10kBurstBenchmark();

            // 2. 100,000 Burst Ingestion & Allocations
            Run100kBurstBenchmark();

            // 3. CoalesceByKey In-Place Fast-Path Throughput
            RunCoalesceByKeyBenchmark();

            // 4. History Storage Query Throughput
            RunHistoryStorageBenchmark();

            Console.WriteLine();
            Console.WriteLine("All benchmarks completed successfully.");
        }

        private static void WarmUp()
        {
            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.CoalesceByKey,
                MaxPendingQueueCapacity = 1000
            };
            var queue = new BoundedPendingQueue(1000);
            var admission = new BoundedAdmissionController(config, queue);

            for (int i = 0; i < 100; i++)
            {
                var model = CreateModel(i, "Warmup", "Warmup");
                admission.Admit(model);
            }
        }

        private static void Run10kBurstBenchmark()
        {
            const int count = 10000;
            Console.WriteLine($"--- Benchmark 1: {count:N0} Burst Multi-Threaded Submissions ---");

            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.Drop,
                MaxPendingQueueCapacity = count * 2
            };
            var queue = new BoundedPendingQueue(count * 2);
            var admission = new BoundedAdmissionController(config, queue);

            long startAllocated = GC.GetAllocatedBytesForCurrentThread();
            int gc0Start = GC.CollectionCount(0);
            int gc1Start = GC.CollectionCount(1);
            int gc2Start = GC.CollectionCount(2);

            var sw = Stopwatch.StartNew();

            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                var model = CreateModel(i, "Title " + i, string.Empty);
                admission.Admit(model);
            });

            sw.Stop();

            int gc0 = GC.CollectionCount(0) - gc0Start;
            int gc1 = GC.CollectionCount(1) - gc1Start;
            int gc2 = GC.CollectionCount(2) - gc2Start;

            double opsPerSec = count / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Throughput: {opsPerSec:N0} notifications/sec");
            Console.WriteLine($"GC Collections: Gen0={gc0}, Gen1={gc1}, Gen2={gc2}");
            Console.WriteLine($"Target 10,000/sec Met: {(opsPerSec >= 10000 ? "YES (PASSED)" : "NO")}");
            Console.WriteLine();
        }

        private static void Run100kBurstBenchmark()
        {
            const int count = 100000;
            Console.WriteLine($"--- Benchmark 2: {count:N0} Burst Ingestion & Memory Growth ---");

            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.Drop,
                MaxPendingQueueCapacity = count * 2
            };
            var queue = new BoundedPendingQueue(count * 2);
            var admission = new BoundedAdmissionController(config, queue);

            long memBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();

            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                var model = CreateModel(i, "100k Burst " + i, string.Empty);
                admission.Admit(model);
            });

            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            long memGrowth = Math.Max(0, memAfter - memBefore);

            double opsPerSec = count / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Throughput: {opsPerSec:N0} notifications/sec");
            Console.WriteLine($"Approx Memory Growth: {memGrowth / (1024.0 * 1024.0):F2} MB");
            Console.WriteLine();
        }

        private static void RunCoalesceByKeyBenchmark()
        {
            const int count = 50000;
            Console.WriteLine($"--- Benchmark 3: CoalesceByKey Fast-Path (50,000 repeated key submissions) ---");

            var config = new NotificationConfig
            {
                BurstStrategy = BurstStrategy.CoalesceByKey,
                GroupingStrategy = NotificationGroupingStrategy.ByGroupKey,
                MaxPendingQueueCapacity = 10000
            };
            var queue = new BoundedPendingQueue(10000);
            var admission = new BoundedAdmissionController(config, queue);

            // Pre-seed 10 keys
            for (int k = 0; k < 10; k++)
            {
                admission.Admit(CreateModel(k, "Title " + k, "Key_" + k));
            }

            var sw = Stopwatch.StartNew();

            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                string key = "Key_" + (i % 10);
                var model = CreateModel(i, "Title", key);
                admission.Admit(model);
            });

            sw.Stop();

            double opsPerSec = count / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Throughput: {opsPerSec:N0} coalesced updates/sec");
            Console.WriteLine($"Queue Node Count: {queue.Count} (bounded, zero queue node proliferation)");
            Console.WriteLine();
        }

        private static void RunHistoryStorageBenchmark()
        {
            const int count = 20000;
            Console.WriteLine($"--- Benchmark 4: History Storage Insertion & Query Throughput ({count:N0} items) ---");

            var storage = new MemoryStorage();
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                storage.Append(new NotificationSnapshot(
                    i,
                    NotificationType.Info,
                    "Title " + i,
                    "Message " + i,
                    DateTime.UtcNow,
                    false,
                    1,
                    "Key"));
            }

            sw.Stop();
            double insertOps = count / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Insert Elapsed: {sw.ElapsedMilliseconds} ms ({insertOps:N0} inserts/sec)");

            sw.Restart();
            for (int q = 0; q < 1000; q++)
            {
                var latest = storage.GetLatest(10);
            }
            sw.Stop();
            double queryOps = 1000 / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"1,000 GetLatest(10) Queries Elapsed: {sw.ElapsedMilliseconds} ms ({queryOps:N0} queries/sec)");
            Console.WriteLine();
        }

        private static NotificationModel CreateModel(long id, string title, string groupKey)
        {
            return new NotificationModel(
                id,
                NotificationType.Info,
                title,
                "Message payload",
                null,
                null,
                "ModernCard",
                NotificationPriority.Info,
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
